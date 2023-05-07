// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemSparseRecord
{
    public short AllowableClass;
    public long AllowableRace;
    public byte ArtifactID;
    public uint BagFamily;
    public byte Bonding;
    public uint BuyPrice;
    public byte ContainerSlots;
    public uint ContentTuningID;
    public byte DamageType;
    public string Description;
    public LocalizedString Display;
    public string Display1;
    public string Display2;
    public string Display3;
    public float DmgVariance;
    public uint DurationInInventory;
    public int ExpansionID;
    public uint FactionRelated;
    public int[] Flags = new int[4];
    public ushort GemProperties;
    public uint Id;
    public ushort InstanceBound;
    public InventoryType inventoryType;
    public ushort ItemDelay;
    public ushort ItemLevel;
    public ushort ItemNameDescriptionID;
    public float ItemRange;
    public ushort ItemSet;
    public int LanguageID;
    public uint LimitCategory;
    public ushort LockID;
    public byte Material;
    public uint MaxCount;
    public ushort MinFactionID;
    public uint MinReputation;
    public int ModifiedCraftingReagentItemID;
    public sbyte OverallQualityID;
    public ushort PageID;
    public byte PageMaterialID;
    public uint PlayerLevelToItemLevelCurveID;
    public float PriceRandomValue;
    public float PriceVariance;
    public float QualityModifier;
    public uint RequiredAbility;
    public ushort RequiredHoliday;
    public sbyte RequiredLevel;
    public byte RequiredPVPMedal;
    public byte RequiredPVPRank;
    public ushort RequiredSkill;
    public ushort RequiredSkillRank;
    public ushort RequiredTransmogHoliday;
    public uint SellPrice;
    public byte SheatheType;
    public ushort SocketMatchEnchantmentId;
    public byte[] SocketType = new byte[ItemConst.MaxGemSockets];
    public byte SpellWeight;
    public byte SpellWeightCategory;
    public uint Stackable;
    public uint StartQuestID;
    public sbyte[] StatModifierBonusStat = new sbyte[ItemConst.MaxStats];
    public float[] StatPercentageOfSocket = new float[ItemConst.MaxStats];
    public int[] StatPercentEditor = new int[ItemConst.MaxStats];
    public ushort TotemCategoryID;
    public uint VendorStackCount;
    public ushort[] ZoneBound = new ushort[2];
}