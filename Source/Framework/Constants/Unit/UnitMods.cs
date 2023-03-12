// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum UnitMods
{
	StatStrength, // STAT_STRENGTH..UNIT_MOD_STAT_INTELLECT must be in existed order, it's accessed by index values of Stats enum.
	StatAgility,
	StatStamina,
	StatIntellect,
	Health,
	Mana, // UNIT_MOD_MANA..UNIT_MOD_PAIN must be listed in existing order, it is accessed by index values of Powers enum.
	Rage,
	Focus,
	Energy,
	ComboPoints,
	Runes,
	RunicPower,
	SoulShards,
	LunarPower,
	HolyPower,
	Alternate,
	Maelstrom,
	Chi,
	Insanity,
	BurningEmbers,
	DemonicFury,
	ArcaneCharges,
	Fury,
	Pain,
	Essence,
	Armor, // ARMOR..RESISTANCE_ARCANE must be in existed order, it's accessed by index values of SpellSchools enum.
	ResistanceHoly,
	ResistanceFire,
	ResistanceNature,
	ResistanceFrost,
	ResistanceShadow,
	ResistanceArcane,
	AttackPower,
	AttackPowerRanged,
	DamageMainHand,
	DamageOffHand,
	DamageRanged,
	End,

	// synonyms
	StatStart = StatStrength,
	StatEnd = StatIntellect + 1,
	ResistanceStart = Armor,
	ResistanceEnd = ResistanceArcane + 1,
	PowerStart = Mana,
	PowerEnd = Essence + 1
}