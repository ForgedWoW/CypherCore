// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Spells;

namespace Forged.RealmServer.Entities;

public class SpellPeriodicAuraLogInfo
{
	public AuraEffect AuraEff { get; set; }
	public double Damage { get; set; }
	public double OriginalDamage { get; set; }
	public double OverDamage { get; set; } // overkill/overheal
	public double Absorb { get; set; }
	public double Resist { get; set; }
	public double Multiplier { get; set; }
	public bool Critical { get; set; }

	public SpellPeriodicAuraLogInfo(AuraEffect _auraEff, double _damage, double _originalDamage, double _overDamage, double _absorb, double _resist, double _multiplier, bool _critical)
	{
		AuraEff = _auraEff;
		Damage = _damage;
		OriginalDamage = _originalDamage;
		OverDamage = _overDamage;
		Absorb = _absorb;
		Resist = _resist;
		Multiplier = _multiplier;
		Critical = _critical;
	}
}