// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Spells;

namespace Forged.RealmServer.Entities;

public class CalcDamageInfo
{
	public double Damage;
	public double Absorb;
	public Unit Attacker { get; set; } // Attacker
	public Unit Target { get; set; }   // Target for damage
	public uint DamageSchoolMask { get; set; }
	public double OriginalDamage { get; set; }
	public double Resist { get; set; }
	public double Blocked { get; set; }
	public HitInfo HitInfo { get; set; }
	public VictimState TargetState { get; set; }

	// Helper
	public WeaponAttackType AttackType { get; set; }
	public ProcFlagsInit ProcAttacker { get; set; }
	public ProcFlagsInit ProcVictim { get; set; }
	public double CleanDamage { get; set; }         // Used only for rage calculation
	public MeleeHitOutcome HitOutCome { get; set; } // TODO: remove this field (need use TargetState)
}