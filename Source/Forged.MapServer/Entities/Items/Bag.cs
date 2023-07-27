// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Networking;
using Framework.Constants;
using Framework.Database;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Entities.Items;

public class Bag : Item
{
    private readonly ContainerData _containerData;
    private Item[] _bagSlot = new Item[36];

    public Bag(ClassFactory classFactory, ItemFactory itemFactory, DB2Manager db2Manager, PlayerComputators playerComputators, CharacterDatabase characterDatabase, LootItemStorage lootItemStorage, 
               ItemEnchantmentManager itemEnchantmentManager, DB6Storage<ItemEffectRecord> itemEffectRecords, ItemTemplateCache itemTemplateCache)
        : base(classFactory, itemFactory, db2Manager, playerComputators, characterDatabase, lootItemStorage, itemEnchantmentManager, itemEffectRecords, itemTemplateCache)
    {
        ObjectTypeMask |= TypeMask.Container;
        ObjectTypeId = TypeId.Container;

        _containerData = new ContainerData();
    }

    public override void AddToWorld()
    {
        base.AddToWorld();

        for (uint i = 0; i < GetBagSize(); ++i)
            if (_bagSlot[i] != null)
                _bagSlot[i].AddToWorld();
    }

    public override void BuildCreateUpdateBlockForPlayer(UpdateData data, Player target)
    {
        base.BuildCreateUpdateBlockForPlayer(data, target);

        for (var i = 0; i < GetBagSize(); ++i)
            if (_bagSlot[i] != null)
                _bagSlot[i].BuildCreateUpdateBlockForPlayer(data, target);
    }

    public override void BuildValuesCreate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        buffer.WriteUInt8((byte)flags);
        ObjectData.WriteCreate(buffer, flags, this, target);
        ItemData.WriteCreate(buffer, flags, this, target);
        _containerData.WriteCreate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteBytes(buffer);
    }

    public override void BuildValuesUpdate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        buffer.WriteUInt32(Values.GetChangedObjectTypeMask());

        if (Values.HasChanged(TypeId.Object))
            ObjectData.WriteUpdate(buffer, flags, this, target);

        if (Values.HasChanged(TypeId.Item))
            ItemData.WriteUpdate(buffer, flags, this, target);

        if (Values.HasChanged(TypeId.Container))
            _containerData.WriteUpdate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteBytes(buffer);
    }

    public override void ClearUpdateMask(bool remove)
    {
        Values.ClearChangesMask(_containerData);
        base.ClearUpdateMask(remove);
    }

    public override bool Create(ulong guidlow, uint itemid, ItemContext context, Player owner)
    {
        var itemProto = ItemTemplateCache.GetItemTemplate(itemid);

        if (itemProto == null || itemProto.ContainerSlots > ItemConst.MaxBagSize)
            return false;

        Create(ObjectGuid.Create(HighGuid.Item, guidlow));

        BonusData = new BonusData(itemProto, DB2Manager, ItemEffectRecords);

        Entry = itemid;
        ObjectScale = 1.0f;

        if (owner != null)
        {
            SetOwnerGUID(owner.GUID);
            SetContainedIn(owner.GUID);
        }

        SetUpdateFieldValue(Values.ModifyValue(ItemData).ModifyValue(ItemData.MaxDurability), itemProto.MaxDurability);
        SetDurability(itemProto.MaxDurability);
        SetCount(1);
        SetContext(context);

        // Setting the number of Slots the Container has
        SetBagSize(itemProto.ContainerSlots);

        // Cleaning 20 slots
        for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
            SetSlot(i, ObjectGuid.Empty);

        _bagSlot = new Item[ItemConst.MaxBagSize];

        return true;
    }

    public override void DeleteFromDB(SQLTransaction trans)
    {
        for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
            if (_bagSlot[i] != null)
                _bagSlot[i].DeleteFromDB(trans);

        base.DeleteFromDB(trans);
    }

    public override void Dispose()
    {
        for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
        {
            var item = _bagSlot[i];

            if (item == null)
                continue;

            if (item.Location.IsInWorld)
            {
                Log.Logger.Fatal("Item {0} (slot {1}, bag slot {2}) in bag {3} (slot {4}, bag slot {5}, m_bagslot {6}) is to be deleted but is still in world.",
                                 item.Entry,
                                 item.Slot,
                                 item.BagSlot,
                                 Entry,
                                 Slot,
                                 BagSlot,
                                 i);

                item.RemoveFromWorld();
            }

            _bagSlot[i].Dispose();
        }

        base.Dispose();
    }

    public uint GetBagSize()
    {
        return _containerData.NumSlots;
    }

    public uint GetFreeSlots()
    {
        uint slots = 0;

        for (uint i = 0; i < GetBagSize(); ++i)
            if (_bagSlot[i] == null)
                ++slots;

        return slots;
    }

    public Item GetItemByPos(byte slot)
    {
        return slot < GetBagSize() ? _bagSlot[slot] : null;
    }

    public bool IsEmpty()
    {
        for (var i = 0; i < GetBagSize(); ++i)
            if (_bagSlot[i] != null)
                return false;

        return true;
    }

    public override bool LoadFromDB(ulong guid, ObjectGuid ownerGUID, SQLFields fields, uint entry)
    {
        if (!base.LoadFromDB(guid, ownerGUID, fields, entry))
            return false;

        var itemProto = Template; // checked in Item.LoadFromDB
        SetBagSize(itemProto.ContainerSlots);

        // cleanup bag content related item value fields (its will be filled correctly from `character_inventory`)
        for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
        {
            SetSlot(i, ObjectGuid.Empty);
            _bagSlot[i] = null;
        }

        return true;
    }

    public override void RemoveFromWorld()
    {
        for (uint i = 0; i < GetBagSize(); ++i)
            if (_bagSlot[i] != null)
                _bagSlot[i].RemoveFromWorld();

        base.RemoveFromWorld();
    }

    public void RemoveItem(byte slot, bool update)
    {
        if (_bagSlot[slot] != null)
            _bagSlot[slot].SetContainer(null);

        _bagSlot[slot] = null;
        SetSlot(slot, ObjectGuid.Empty);
    }

    public void StoreItem(byte slot, Item pItem, bool update)
    {
        if (pItem == null || pItem.GUID == GUID)
            return;

        _bagSlot[slot] = pItem;
        SetSlot(slot, pItem.GUID);
        pItem.SetContainedIn(GUID);
        pItem.SetOwnerGUID(OwnerGUID);
        pItem.SetContainer(this);
        pItem.SetSlot(slot);
    }

    private void SetBagSize(uint numSlots)
    {
        SetUpdateFieldValue(Values.ModifyValue(_containerData).ModifyValue(_containerData.NumSlots), numSlots);
    }

    private void SetSlot(int slot, ObjectGuid guid)
    {
        SetUpdateFieldValue(ref Values.ModifyValue(_containerData).ModifyValue(_containerData.Slots, slot), guid);
    }
}