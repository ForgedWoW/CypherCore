// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Spells.Auras;

namespace Forged.MapServer.Entities.Units;

public class SpellPeriodicAuraLogInfo
{
    public SpellPeriodicAuraLogInfo(AuraEffect auraEff, double damage, double originalDamage, double overDamage, double absorb, double resist, double multiplier, bool critical)
    {
        AuraEff = auraEff;
        Damage = damage;
        OriginalDamage = originalDamage;
        OverDamage = overDamage;
        Absorb = absorb;
        Resist = resist;
        Multiplier = multiplier;
        Critical = critical;
    }

    public double Absorb { get; set; }
    public AuraEffect AuraEff { get; set; }
    public bool Critical { get; set; }
    public double Damage { get; set; }
    public double Multiplier { get; set; }
    public double OriginalDamage { get; set; }
    public double OverDamage { get; set; } // overkill/overheal
    public double Resist { get; set; }
}