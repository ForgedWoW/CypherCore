// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum ItemFlags : long
{
	NoPickup = 0x01,
	Conjured = 0x02,        // Conjured Item
	HasLoot = 0x04,         // Item Can Be Right Clicked To Open For Loot
	HeroicTooltip = 0x08,   // Makes Green "Heroic" Text Appear On Item
	Deprecated = 0x10,      // Cannot Equip Or Use
	NoUserDestroy = 0x20,   // Item Can Not Be Destroyed, Except By Using Spell (Item Can Be Reagent For Spell)
	Playercast = 0x40,      // Item's spells are castable by players
	NoEquipCooldown = 0x80, // No Default 30 Seconds Cooldown When Equipped
	Legacy = 0x100,         // Effects are disabled
	IsWrapper = 0x200,      // Item Can Wrap Other Items
	UsesResources = 0x400,
	MultiDrop = 0x800,           // Looting This Item Does Not Remove It From Available Loot
	ItemPurchaseRecord = 0x1000, // Item Can Be Returned To Vendor For Its Original Cost (Extended Cost)
	Petition = 0x2000,           // Item Is Guild Or Arena Charter
	HasText = 0x4000,            // Only readable items have this (but not all)
	NoDisenchant = 0x8000,
	RealDuration = 0x10000,
	NoCreator = 0x20000,
	IsProspectable = 0x40000,                  // Item Can Be Prospected
	UniqueEquippable = 0x80000,                // You Can Only Equip One Of These
	DisableAutoQuotes = 0x100000,              // Disables quotes around item description in tooltip
	IgnoreDefaultArenaRestrictions = 0x200000, // Item Can Be Used During Arena Match
	NoDurabilityLoss = 0x400000,               // Some Thrown weapons have it (and only Thrown) but not all
	UseWhenShapeshifted = 0x800000,            // Item Can Be Used In Shapeshift Forms
	HasQuestGlow = 0x1000000,
	HideUnusableRecipe = 0x2000000, // Profession Recipes: Can Only Be Looted If You Meet Requirements And Don'T Already Know It
	NotUseableInArena = 0x4000000,  // Item Cannot Be Used In Arena
	IsBoundToAccount = 0x8000000,   // Item Binds To Account And Can Be Sent Only To Your Own Characters
	NoReagentCost = 0x10000000,     // Spell Is Cast Ignoring Reagents
	IsMillable = 0x20000000,        // Item Can Be Milled
	ReportToGuildChat = 0x40000000,
	NoProgressiveLoot = 0x80000000
}