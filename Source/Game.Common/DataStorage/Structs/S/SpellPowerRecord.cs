// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.DataStorage;

public sealed class SpellPowerRecord
{
	public uint Id;
	public byte OrderIndex;
	public int ManaCost;
	public int ManaCostPerLevel;
	public int ManaPerSecond;
	public uint PowerDisplayID;
	public int AltPowerBarID;
	public float PowerCostPct;
	public float PowerCostMaxPct;
	public float OptionalCostPct;
	public float PowerPctPerSecond;
	public PowerType PowerType;
	public uint RequiredAuraSpellID;

	public uint OptionalCost; // Spell uses [ManaCost, ManaCost+ManaCostAdditional] power - affects tooltip parsing as multiplier on SpellEffectEntry::EffectPointsPerResource

	//   only SPELL_EFFECT_WEAPON_DAMAGE_NOSCHOOL, SPELL_EFFECT_WEAPON_PERCENT_DAMAGE, SPELL_EFFECT_WEAPON_DAMAGE, SPELL_EFFECT_NORMALIZED_WEAPON_DMG
	public uint SpellID;
}