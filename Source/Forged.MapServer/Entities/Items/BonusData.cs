// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Entities.Items;

public class BonusData
{
    public uint AppearanceModID;
    public uint AzeriteTierUnlockSetId;
    public ItemBondingType Bonding;
    public bool CanDisenchant;
    public bool CanScrap;
    public uint ContentTuningId;
    public uint DisenchantLootId;
    public int EffectCount;
    public ItemEffectRecord[] Effects = new ItemEffectRecord[13];
    public uint[] GemItemLevelBonus = new uint[ItemConst.MaxGemSockets];
    public ushort[] GemRelicRankBonus = new ushort[ItemConst.MaxGemSockets];
    public int[] GemRelicType = new int[ItemConst.MaxGemSockets];
    public bool HasFixedLevel;
    public int ItemLevelBonus;
    public float[] ItemStatSocketCostMultiplier = new float[ItemConst.MaxStats];
    public int[] ItemStatType = new int[ItemConst.MaxStats];
    public uint PlayerLevelToItemLevelCurveId;
    public ItemQuality Quality;
    public int RelicType;
    public float RepairCostMultiplier;
    public int RequiredLevel;
    public uint RequiredLevelCurve;
    public int RequiredLevelOverride;
    public SocketColor[] socketColor = new SocketColor[ItemConst.MaxGemSockets];
    public int[] StatPercentEditor = new int[ItemConst.MaxStats];
    public uint Suffix;
    private State _state;

    public BonusData(ItemTemplate proto)
    {
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
            socketColor[i] = proto.GetSocketColor(i);
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

        var azeriteEmpoweredItem = Global.DB2Mgr.GetAzeriteEmpoweredItem(proto.Id);

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

    public BonusData(ItemInstance itemInstance) : this(Global.ObjectMgr.GetItemTemplate(itemInstance.ItemID))
    {
        if (itemInstance.ItemBonus != null)
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
                    if (socketColor[i] == 0)
                    {
                        socketColor[i] = (SocketColor)values[1];
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
                if (CliDB.ItemEffectStorage.TryGetValue(values[0], out var itemEffect))
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
        var bonuses = Global.DB2Mgr.GetItemBonusList(bonusListId);

        if (bonuses != null)
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