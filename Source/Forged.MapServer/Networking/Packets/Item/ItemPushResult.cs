// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Crafting;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

internal class ItemPushResult : ServerPacket
{
    public int BattlePetBreedID;

    public uint BattlePetBreedQuality;

    public int BattlePetLevel;

    public int BattlePetSpeciesID;

    public CraftingData CraftingData;

    public bool Created;

    public DisplayType DisplayText;

    public int DungeonEncounterID;

    public uint? FirstCraftOperationID;

    public bool IsBonusRoll;

    public bool IsEncounterLoot;

    public ItemInstance Item;

    public ObjectGuid ItemGUID;

    public ObjectGuid PlayerGUID;

    public bool Pushed;

    // only set if different than real ID (similar to CreatureTemplate.KillCredit)
    public uint Quantity;

    public uint QuantityInInventory;

    public int QuestLogItemID;

    public byte Slot;

    public int SlotInBag;

    // Item ID used for updating quest progress
    public List<UiEventToast> Toasts = new();

    public ItemPushResult() : base(ServerOpcodes.ItemPushResult) { }

    public enum DisplayType
    {
        Hidden = 0,
        Normal = 1,
        EncounterLoot = 2
    }
    public override void Write()
    {
        _worldPacket.WritePackedGuid(PlayerGUID);
        _worldPacket.WriteUInt8(Slot);
        _worldPacket.WriteInt32(SlotInBag);
        _worldPacket.WriteInt32(QuestLogItemID);
        _worldPacket.WriteUInt32(Quantity);
        _worldPacket.WriteUInt32(QuantityInInventory);
        _worldPacket.WriteInt32(DungeonEncounterID);
        _worldPacket.WriteInt32(BattlePetSpeciesID);
        _worldPacket.WriteInt32(BattlePetBreedID);
        _worldPacket.WriteUInt32(BattlePetBreedQuality);
        _worldPacket.WriteInt32(BattlePetLevel);
        _worldPacket.WritePackedGuid(ItemGUID);
        _worldPacket.WriteInt32(Toasts.Count);

        foreach (var uiEventToast in Toasts)
            uiEventToast.Write(_worldPacket);

        _worldPacket.WriteBit(Pushed);
        _worldPacket.WriteBit(Created);
        _worldPacket.WriteBits((uint)DisplayText, 3);
        _worldPacket.WriteBit(IsBonusRoll);
        _worldPacket.WriteBit(IsEncounterLoot);
        _worldPacket.WriteBit(CraftingData != null);
        _worldPacket.WriteBit(FirstCraftOperationID.HasValue);
        _worldPacket.FlushBits();

        Item.Write(_worldPacket);

        if (FirstCraftOperationID.HasValue)
            _worldPacket.WriteUInt32(FirstCraftOperationID.Value);

        CraftingData?.Write(_worldPacket);
    }
}