// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Entities.Items;

public class BonusData
{
    private readonly DB2Manager _db2Manager;
    private readonly DB6Storage<ItemEffectRecord> _itemEffectRecords;
    public uint AppearanceModID { get; set; }
    public uint AzeriteTierUnlockSetId { get; set; }
    public ItemBondingType Bonding { get; set; }
    public bool CanDisenchant { get; set; }
    public bool CanScrap { get; set; }
    public uint ContentTuningId { get; set; }
    public uint DisenchantLootId { get; set; }
    public int EffectCount { get; set; }
    public ItemEffectRecord[] Effects { get; set; } = new ItemEffectRecord[13];
    public uint[] GemItemLevelBonus { get; set; } = new uint[ItemConst.MaxGemSockets];
    public ushort[] GemRelicRankBonus { get; set; } = new ushort[ItemConst.MaxGemSockets];
    public int[] GemRelicType { get; set; } = new int[ItemConst.MaxGemSockets];
    public bool HasFixedLevel { get; set; }
    public int ItemLevelBonus { get; set; }
    public float[] ItemStatSocketCostMultiplier { get; set; } = new float[ItemConst.MaxStats];
    public int[] ItemStatType { get; set; } = new int[ItemConst.MaxStats];
    public uint PlayerLevelToItemLevelCurveId { get; set; }
    public ItemQuality Quality { get; set; }
    public int RelicType { get; set; }
    public float RepairCostMultiplier { get; set; }
    public int RequiredLevel { get; set; }
    public uint RequiredLevelCurve { get; set; }
    public int RequiredLevelOverride { get; set; }
    public SocketColor[] SocketColor { get; set; } = new SocketColor[ItemConst.MaxGemSockets];
    public int[] StatPercentEditor { get; set; } = new int[ItemConst.MaxStats];
    public uint Suffix { get; set; }
    private State _state;

    public BonusData(ItemTemplate proto, DB2Manager db2Manager, DB6Storage<ItemEffectRecord> itemEffectRecords)
    {
        _db2Manager = db2Manager;
        _itemEffectRecords = itemEffectRecords;

        if (proto == null)
            return;

        Quality = proto.Quality;
        ItemLevelBonus = 0;
        RequiredLevel = proto.BaseRequiredLevel;

        for (uint i = 0; i < ItemConst.MaxStats; ++i)
            ItemStatType[i] = proto.GetStatModifierBonusStat(i);

        for (uint i = 0; i < ItemConst.MaxStats; ++i)
            StatPercentEditor[i] = proto.GetStatPercentEditor(i);

        for (uint i = 0; i < ItemConst.MaxStats; ++i)
            ItemStatSocketCostMultiplier[i] = proto.GetStatPercentageOfSocket(i);

        for (uint i = 0; i < ItemConst.MaxGemSockets; ++i)
        {
            SocketColor[i] = proto.GetSocketColor(i);
            GemItemLevelBonus[i] = 0;
            GemRelicType[i] = -1;
            GemRelicRankBonus[i] = 0;
        }

        Bonding = proto.Bonding;

        AppearanceModID = 0;
        RepairCostMultiplier = 1.0f;
        ContentTuningId = proto.ScalingStatContentTuning;
        PlayerLevelToItemLevelCurveId = proto.PlayerLevelToItemLevelCurveId;
        RelicType = -1;
        HasFixedLevel = false;
        RequiredLevelOverride = 0;
        AzeriteTierUnlockSetId = 0;

        var azeriteEmpoweredItem = db2Manager.GetAzeriteEmpoweredItem(proto.Id);

        if (azeriteEmpoweredItem != null)
            AzeriteTierUnlockSetId = azeriteEmpoweredItem.AzeriteTierUnlockSetID;

        EffectCount = 0;

        foreach (var itemEffect in proto.Effects)
            Effects[EffectCount++] = itemEffect;

        for (var i = EffectCount; i < Effects.Length; ++i)
            Effects[i] = null;

        CanDisenchant = !proto.HasFlag(ItemFlags.NoDisenchant);
        CanScrap = proto.HasFlag(ItemFlags4.Scrapable);

        _state.SuffixPriority = int.MaxValue;
        _state.AppearanceModPriority = int.MaxValue;
        _state.ScalingStatDistributionPriority = int.MaxValue;
        _state.AzeriteTierUnlockSetPriority = int.MaxValue;
        _state.RequiredLevelCurvePriority = int.MaxValue;
        _state.HasQualityBonus = false;
    }

    public BonusData(ItemInstance itemInstance, DB2Manager db2Manager, DB6Storage<ItemEffectRecord> itemEffectRecords, GameObjectManager objectManager) : this(objectManager.GetItemTemplate(itemInstance.ItemID), db2Manager, itemEffectRecords)
    {
        if (itemInstance.ItemBonus == null)
            return;

        foreach (var bonusListID in itemInstance.ItemBonus.BonusListIDs)
            AddBonusList(bonusListID);
    }

    public void AddBonus(ItemBonusType type, int[] values)
    {
        switch (type)
        {
            case ItemBonusType.ItemLevel:
                ItemLevelBonus += values[0];

                break;

            case ItemBonusType.Stat:
            {
                uint statIndex;

                for (statIndex = 0; statIndex < ItemConst.MaxStats; ++statIndex)
                    if (ItemStatType[statIndex] == values[0] || ItemStatType[statIndex] == -1)
                        break;

                if (statIndex < ItemConst.MaxStats)
                {
                    ItemStatType[statIndex] = values[0];
                    StatPercentEditor[statIndex] += values[1];
                }

                break;
            }
            case ItemBonusType.Quality:
                if (!_state.HasQualityBonus)
                {
                    Quality = (ItemQuality)values[0];
                    _state.HasQualityBonus = true;
                }
                else if ((uint)Quality < values[0])
                    Quality = (ItemQuality)values[0];

                break;

            case ItemBonusType.Suffix:
                if (values[1] < _state.SuffixPriority)
                {
                    Suffix = (uint)values[0];
                    _state.SuffixPriority = values[1];
                }

                break;

            case ItemBonusType.Socket:
            {
                var socketCount = (uint)values[0];

                for (uint i = 0; i < ItemConst.MaxGemSockets && socketCount != 0; ++i)
                    if (SocketColor[i] == 0)
                    {
                        SocketColor[i] = (SocketColor)values[1];
                        --socketCount;
                    }

                break;
            }
            case ItemBonusType.Appearance:
                if (values[1] < _state.AppearanceModPriority)
                {
                    AppearanceModID = Convert.ToUInt32(values[0]);
                    _state.AppearanceModPriority = values[1];
                }

                break;

            case ItemBonusType.RequiredLevel:
                RequiredLevel += values[0];

                break;

            case ItemBonusType.RepairCostMuliplier:
                RepairCostMultiplier *= Convert.ToSingle(values[0]) * 0.01f;

                break;

            case ItemBonusType.ScalingStatDistribution:
            case ItemBonusType.ScalingStatDistributionFixed:
                if (values[1] < _state.ScalingStatDistributionPriority)
                {
                    ContentTuningId = (uint)values[2];
                    PlayerLevelToItemLevelCurveId = (uint)values[3];
                    _state.ScalingStatDistributionPriority = values[1];
                    HasFixedLevel = type == ItemBonusType.ScalingStatDistributionFixed;
                }

                break;

            case ItemBonusType.Bounding:
                Bonding = (ItemBondingType)values[0];

                break;

            case ItemBonusType.RelicType:
                RelicType = values[0];

                break;

            case ItemBonusType.OverrideRequiredLevel:
                RequiredLevelOverride = values[0];

                break;

            case ItemBonusType.AzeriteTierUnlockSet:
                if (values[1] < _state.AzeriteTierUnlockSetPriority)
                {
                    AzeriteTierUnlockSetId = (uint)values[0];
                    _state.AzeriteTierUnlockSetPriority = values[1];
                }

                break;

            case ItemBonusType.OverrideCanDisenchant:
                CanDisenchant = values[0] != 0;

                break;

            case ItemBonusType.OverrideCanScrap:
                CanScrap = values[0] != 0;

                break;

            case ItemBonusType.ItemEffectId:
                if (_itemEffectRecords.TryGetValue((uint)values[0], out var itemEffect))
                    Effects[EffectCount++] = itemEffect;

                break;

            case ItemBonusType.RequiredLevelCurve:
                if (values[2] < _state.RequiredLevelCurvePriority)
                {
                    RequiredLevelCurve = (uint)values[0];
                    _state.RequiredLevelCurvePriority = values[2];

                    if (values[1] != 0)
                        ContentTuningId = (uint)values[1];
                }

                break;
        }
    }

    public void AddBonusList(uint bonusListId)
    {
        var bonuses = _db2Manager.GetItemBonusList(bonusListId);

        if (bonuses == null)
            return;

        foreach (var bonus in bonuses)
            AddBonus(bonus.BonusType, bonus.Value);
    }

    private struct State
    {
        public int AppearanceModPriority;
        public int AzeriteTierUnlockSetPriority;
        public bool HasQualityBonus;
        public int RequiredLevelCurvePriority;
        public int ScalingStatDistributionPriority;
        public int SuffixPriority;
    }
}