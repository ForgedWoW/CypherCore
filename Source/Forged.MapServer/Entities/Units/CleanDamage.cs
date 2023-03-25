// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

public class CleanDamage
{
	public double AbsorbedDamage { get; }
	public double MitigatedDamage { get; set; }

	public WeaponAttackType AttackType { get; }
	public MeleeHitOutcome HitOutCome { get; }

	public CleanDamage(double mitigated, double absorbed, WeaponAttackType attackType, MeleeHitOutcome hitOutCome)
	{
		AbsorbedDamage = absorbed;
		MitigatedDamage = mitigated;
		AttackType = attackType;
		HitOutCome = hitOutCome;
	}
}