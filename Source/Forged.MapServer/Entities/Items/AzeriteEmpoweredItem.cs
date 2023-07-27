// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Networking;
using Framework.Constants;
using Framework.Database;
using Game.Common;

namespace Forged.MapServer.Entities.Items;

public class AzeriteEmpoweredItem : Item
{
    private readonly AzeriteEmpoweredItemData _azeriteEmpoweredItemData;
    private readonly AzeriteEmpoweredItemFactory _azeriteEmpoweredItemFactory;
    private List<AzeritePowerSetMemberRecord> _azeritePowers;
    private int _maxTier;

    public AzeriteEmpoweredItem(ClassFactory classFactory, ItemFactory itemFactory, DB2Manager db2Manager, PlayerComputators playerComputators, CharacterDatabase characterDatabase, LootItemStorage lootItemStorage, ItemEnchantmentManager itemEnchantmentManager,
                                AzeriteEmpoweredItemFactory azeriteEmpoweredItemFactory, DB6Storage<ItemEffectRecord> itemEffectRecords, ItemTemplateCache itemTemplateCache)
        : base(classFactory, itemFactory, db2Manager, playerComputators, characterDatabase, lootItemStorage, itemEnchantmentManager, itemEffectRecords, itemTemplateCache)
    {
        _azeriteEmpoweredItemFactory = azeriteEmpoweredItemFactory;
        ObjectTypeMask |= TypeMask.AzeriteEmpoweredItem;
        ObjectTypeId = TypeId.AzeriteEmpoweredItem;

        _azeriteEmpoweredItemData = new AzeriteEmpoweredItemData();
    }

    public override void BuildValuesCreate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        buffer.WriteUInt8((byte)flags);
        ObjectData.WriteCreate(buffer, flags, this, target);
        ItemData.WriteCreate(buffer, flags, this, target);
        _azeriteEmpoweredItemData.WriteCreate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteBytes(buffer);
    }

    public override void BuildValuesUpdate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        if (Values.HasChanged(TypeId.Object))
            ObjectData.WriteUpdate(buffer, flags, this, target);

        if (Values.HasChanged(TypeId.Item))
            ItemData.WriteUpdate(buffer, flags, this, target);

        if (Values.HasChanged(TypeId.AzeriteEmpoweredItem))
            _azeriteEmpoweredItemData.WriteUpdate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteUInt32(Values.GetChangedObjectTypeMask());
        data.WriteBytes(buffer);
    }

    public override void ClearUpdateMask(bool remove)
    {
        Values.ClearChangesMask(_azeriteEmpoweredItemData);
        base.ClearUpdateMask(remove);
    }

    public override bool Create(ulong guidlow, uint itemId, ItemContext context, Player owner)
    {
        if (!base.Create(guidlow, itemId, context, owner))
            return false;

        InitAzeritePowerData();

        return true;
    }

    public override void DeleteFromDB(SQLTransaction trans)
    {
        _azeriteEmpoweredItemFactory.DeleteFromDB(trans, GUID.Counter);
        base.DeleteFromDB(trans);
    }

    public int GetMaxAzeritePowerTier()
    {
        return _maxTier;
    }

    public uint GetRequiredAzeriteLevelForTier(uint tier)
    {
        return DB2Manager.GetRequiredAzeriteLevelForAzeritePowerTier(BonusData.AzeriteTierUnlockSetId, Context, tier);
    }

    public long GetRespecCost()
    {
        var owner = OwnerUnit;

        if (owner != null)
            return (long)(MoneyConstants.Gold * DB2Manager.GetCurveValueAt((uint)Curves.AzeriteEmpoweredItemRespecCost, owner.NumRespecs));

        return (long)PlayerConst.MaxMoneyAmount + 1;
    }

    public uint GetSelectedAzeritePower(int tier)
    {
        return (uint)_azeriteEmpoweredItemData.Selections[tier];
    }

    public int GetTierForAzeritePower(PlayerClass playerClass, int azeritePowerId)
    {
        var azeritePowerItr = _azeritePowers.Find(power => power.AzeritePowerID == azeritePowerId && power.Class == (int)playerClass);

        return azeritePowerItr?.Tier ?? SharedConst.MaxAzeriteEmpoweredTier;
    }

    public void LoadAzeriteEmpoweredItemData(Player owner, AzeriteEmpoweredData azeriteEmpoweredItem)
    {
        InitAzeritePowerData();
        var needSave = false;

        if (_azeritePowers != null)
            for (var i = SharedConst.MaxAzeriteEmpoweredTier; --i >= 0;)
            {
                var selection = azeriteEmpoweredItem.SelectedAzeritePowers[i];

                if (GetTierForAzeritePower(owner.Class, selection) != i)
                {
                    needSave = true;

                    break;
                }

                SetSelectedAzeritePower(i, selection);
            }
        else
            needSave = true;

        if (!needSave)
            return;

        {
            var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_ITEM_INSTANCE_AZERITE_EMPOWERED);

            for (var i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
                stmt.AddValue(i, _azeriteEmpoweredItemData.Selections[i]);

            stmt.AddValue(5, GUID.Counter);
            CharacterDatabase.Execute(stmt);
        }
    }

    public override void SaveToDB(SQLTransaction trans)
    {
        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_EMPOWERED);
        stmt.AddValue(0, GUID.Counter);
        trans.Append(stmt);

        switch (State)
        {
            case ItemUpdateState.New:
            case ItemUpdateState.Changed:
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_AZERITE_EMPOWERED);
                stmt.AddValue(0, GUID.Counter);

                for (var i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
                    stmt.AddValue(1 + i, _azeriteEmpoweredItemData.Selections[i]);

                trans.Append(stmt);

                break;
        }

        base.SaveToDB(trans);
    }

    public void SetSelectedAzeritePower(int tier, int azeritePowerId)
    {
        SetUpdateFieldValue(ref Values.ModifyValue(_azeriteEmpoweredItemData).ModifyValue(_azeriteEmpoweredItemData.Selections, tier), azeritePowerId);

        // Not added to UF::ItemData::BonusListIDs, client fakes it on its own too
        BonusData.AddBonusList(CliDB.AzeritePowerStorage.LookupByKey(azeritePowerId).ItemBonusListID);
    }

    private void InitAzeritePowerData()
    {
        _azeritePowers = DB2Manager.GetAzeritePowers(Entry);

        if (_azeritePowers != null)
            _maxTier = _azeritePowers.Aggregate((a1, a2) => a1.Tier < a2.Tier ? a2 : a1).Tier;
    }
}