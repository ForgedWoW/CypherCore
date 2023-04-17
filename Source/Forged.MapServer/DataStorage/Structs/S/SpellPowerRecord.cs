// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellPowerRecord
{
    public int AltPowerBarID;
    public uint Id;
    public int ManaCost;
    public int ManaCostPerLevel;
    public int ManaPerSecond;
    public uint OptionalCost;
    public float OptionalCostPct;
    public byte OrderIndex;
    public float PowerCostMaxPct;
    public float PowerCostPct;
    public uint PowerDisplayID;
    public float PowerPctPerSecond;
    public PowerType PowerType;
    public uint RequiredAuraSpellID;

    // Spell uses [ManaCost, ManaCost+ManaCostAdditional] power - affects tooltip parsing as multiplier on SpellEffectEntry::EffectPointsPerResource

    //   only SPELL_EFFECT_WEAPON_DAMAGE_NOSCHOOL, SPELL_EFFECT_WEAPON_PERCENT_DAMAGE, SPELL_EFFECT_WEAPON_DAMAGE, SPELL_EFFECT_NORMALIZED_WEAPON_DMG
    public uint SpellID;
}