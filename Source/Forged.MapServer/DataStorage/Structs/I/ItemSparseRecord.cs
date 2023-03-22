﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.DataStorage;

public sealed class ItemSparseRecord
{
	public uint Id;
	public long AllowableRace;
	public string Description;
	public string Display3;
	public string Display2;
	public string Display1;
	public LocalizedString Display;
	public int ExpansionID;
	public float DmgVariance;
	public uint LimitCategory;
	public uint DurationInInventory;
	public float QualityModifier;
	public uint BagFamily;
	public uint StartQuestID;
	public int LanguageID;
	public float ItemRange;
	public float[] StatPercentageOfSocket = new float[ItemConst.MaxStats];
	public int[] StatPercentEditor = new int[ItemConst.MaxStats];
	public uint Stackable;
	public uint MaxCount;
	public uint MinReputation;
	public uint RequiredAbility;
	public uint SellPrice;
	public uint BuyPrice;
	public uint VendorStackCount;
	public float PriceVariance;
	public float PriceRandomValue;
	public int[] Flags = new int[4];
	public uint FactionRelated;
	public int ModifiedCraftingReagentItemID;
	public uint ContentTuningID;
	public uint PlayerLevelToItemLevelCurveID;
	public ushort ItemNameDescriptionID;
	public ushort RequiredTransmogHoliday;
	public ushort RequiredHoliday;
	public ushort GemProperties;
	public ushort SocketMatchEnchantmentId;
	public ushort TotemCategoryID;
	public ushort InstanceBound;
	public ushort[] ZoneBound = new ushort[2];
	public ushort ItemSet;
	public ushort LockID;
	public ushort PageID;
	public ushort ItemDelay;
	public ushort MinFactionID;
	public ushort RequiredSkillRank;
	public ushort RequiredSkill;
	public ushort ItemLevel;
	public short AllowableClass;
	public byte ArtifactID;
	public byte SpellWeight;
	public byte SpellWeightCategory;
	public byte[] SocketType = new byte[ItemConst.MaxGemSockets];
	public byte SheatheType;
	public byte Material;
	public byte PageMaterialID;
	public byte Bonding;
	public byte DamageType;
	public sbyte[] StatModifierBonusStat = new sbyte[ItemConst.MaxStats];
	public byte ContainerSlots;
	public byte RequiredPVPMedal;
	public byte RequiredPVPRank;
	public sbyte RequiredLevel;
	public InventoryType inventoryType;
	public sbyte OverallQualityID;
}