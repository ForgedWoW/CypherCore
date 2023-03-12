// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ItemBonusType : byte
{
	ItemLevel = 1,
	Stat = 2,
	Quality = 3,
	NameSubtitle = 4, // Text under name
	Suffix = 5,
	Socket = 6,
	Appearance = 7,
	RequiredLevel = 8,
	DisplayToastMethod = 9,
	RepairCostMuliplier = 10,
	ScalingStatDistribution = 11,
	DisenchantLootId = 12,
	ScalingStatDistributionFixed = 13,
	ItemLevelCanIncrease = 14, // Displays a + next to item level indicating it can warforge
	RandomEnchantment = 15,    // Responsible for showing "<Random additional stats>" or "+%d Rank Random Minor Trait" in the tooltip before item is obtained
	Bounding = 16,
	RelicType = 17,
	OverrideRequiredLevel = 18,
	AzeriteTierUnlockSet = 19,
	ScrappingLootId = 20,
	OverrideCanDisenchant = 21,
	OverrideCanScrap = 22,
	ItemEffectId = 23,
	ModifiedCraftingStat = 25,
	RequiredLevelCurve = 27,
	DescriptionText = 30, // Item Description
	OverrideName = 31,    // Itemnamedescription Id
	ItemBonusListGroup = 34,
	ItemLimitCategory = 35,
	ItemConversion = 37,
	ItemHistorySlot = 38,
}