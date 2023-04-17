// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

public class CalcDamageInfo
{
    public double Absorb;
    public double Damage;

    public Unit Attacker { get; set; } // Attacker

    // Helper
    public WeaponAttackType AttackType { get; set; }

    public double Blocked { get; set; }
    public double CleanDamage { get; set; }
    public uint DamageSchoolMask { get; set; }

    public HitInfo HitInfo { get; set; }

    // Used only for rage calculation
    public MeleeHitOutcome HitOutCome { get; set; }

    public double OriginalDamage { get; set; }
    public ProcFlagsInit ProcAttacker { get; set; }
    public ProcFlagsInit ProcVictim { get; set; }
    public double Resist { get; set; }
    public Unit Target { get; set; } // Target for damage

    public VictimState TargetState { get; set; }
    // TODO: remove this field (need use TargetState)
}