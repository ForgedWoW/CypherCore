// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

public class DamageInfo
{
    public DamageInfo(Unit attacker, Unit victim, double damage, SpellInfo spellInfo, SpellSchoolMask schoolMask, DamageEffectType damageType, WeaponAttackType attackType)
    {
        Attacker = attacker;
        Victim = victim;
        Damage = damage;
        OriginalDamage = damage;
        SpellInfo = spellInfo;
        SchoolMask = schoolMask;
        DamageType = damageType;
        AttackType = attackType;
    }

    public DamageInfo(CalcDamageInfo dmgInfo)
    {
        Attacker = dmgInfo.Attacker;
        Victim = dmgInfo.Target;
        Damage = dmgInfo.Damage;
        OriginalDamage = dmgInfo.Damage;
        SpellInfo = null;
        SchoolMask = (SpellSchoolMask)dmgInfo.DamageSchoolMask;
        DamageType = DamageEffectType.Direct;
        AttackType = dmgInfo.AttackType;
        Absorb = dmgInfo.Absorb;
        Resist = dmgInfo.Resist;
        Block = dmgInfo.Blocked;

        switch (dmgInfo.TargetState)
        {
            case VictimState.Immune:
                HitMask |= ProcFlagsHit.Immune;

                break;
            case VictimState.Blocks:
                HitMask |= ProcFlagsHit.FullBlock;

                break;
        }

        if (dmgInfo.HitInfo.HasAnyFlag(HitInfo.PartialAbsorb | HitInfo.FullAbsorb))
            HitMask |= ProcFlagsHit.Absorb;

        if (dmgInfo.HitInfo.HasAnyFlag(HitInfo.FullResist))
            HitMask |= ProcFlagsHit.FullResist;

        if (Block != 0)
            HitMask |= ProcFlagsHit.Block;

        var damageNullified = dmgInfo.HitInfo.HasAnyFlag(HitInfo.FullAbsorb | HitInfo.FullResist) || HitMask.HasAnyFlag(ProcFlagsHit.Immune | ProcFlagsHit.FullBlock);

        switch (dmgInfo.HitOutCome)
        {
            case MeleeHitOutcome.Miss:
                HitMask |= ProcFlagsHit.Miss;

                break;
            case MeleeHitOutcome.Dodge:
                HitMask |= ProcFlagsHit.Dodge;

                break;
            case MeleeHitOutcome.Parry:
                HitMask |= ProcFlagsHit.Parry;

                break;
            case MeleeHitOutcome.Evade:
                HitMask |= ProcFlagsHit.Evade;

                break;
            case MeleeHitOutcome.Block:
            case MeleeHitOutcome.Crushing:
            case MeleeHitOutcome.Glancing:
            case MeleeHitOutcome.Normal:
                if (!damageNullified)
                    HitMask |= ProcFlagsHit.Normal;

                break;
            case MeleeHitOutcome.Crit:
                if (!damageNullified)
                    HitMask |= ProcFlagsHit.Critical;

                break;
        }
    }

    public DamageInfo(SpellNonMeleeDamage spellNonMeleeDamage, DamageEffectType damageType, WeaponAttackType attackType, ProcFlagsHit hitMask)
    {
        Attacker = spellNonMeleeDamage.Attacker;
        Victim = spellNonMeleeDamage.Target;
        Damage = spellNonMeleeDamage.Damage;
        SpellInfo = spellNonMeleeDamage.Spell;
        SchoolMask = spellNonMeleeDamage.SchoolMask;
        DamageType = damageType;
        AttackType = attackType;
        Absorb = spellNonMeleeDamage.Absorb;
        Resist = spellNonMeleeDamage.Resist;
        Block = spellNonMeleeDamage.Blocked;
        HitMask = hitMask;

        if (spellNonMeleeDamage.Blocked != 0)
            HitMask |= ProcFlagsHit.Block;

        if (spellNonMeleeDamage.Absorb != 0)
            HitMask |= ProcFlagsHit.Absorb;
    }

    public double Absorb { get; set; }
    public Unit Attacker { get; }

    public WeaponAttackType AttackType { get; }
    public double Block { get; private set; }
    public double Damage { get; set; }
    public DamageEffectType DamageType { get; }
    public ProcFlagsHit HitMask { get; private set; }

    public bool IsAbsorb => HitMask.HasFlag(ProcFlagsHit.Absorb);

    public bool IsBlock => HitMask.HasFlag(ProcFlagsHit.Block);

    public bool IsCritical => HitMask.HasFlag(ProcFlagsHit.Critical);

    public bool IsDodge => HitMask.HasFlag(ProcFlagsHit.Dodge);

    public bool IsEvade => HitMask.HasFlag(ProcFlagsHit.Evade);

    public bool IsFillResist => HitMask.HasFlag(ProcFlagsHit.FullResist);

    public bool IsFullBlock => HitMask.HasFlag(ProcFlagsHit.FullBlock);

    public bool IsImmune => HitMask.HasFlag(ProcFlagsHit.Immune);

    public bool IsMiss => HitMask.HasFlag(ProcFlagsHit.Miss);

    public bool IsNormal => HitMask.HasFlag(ProcFlagsHit.Normal);

    public bool IsParry => HitMask.HasFlag(ProcFlagsHit.Parry);

    public double OriginalDamage { get; }
    public double Resist { get; private set; }
    public SpellSchoolMask SchoolMask { get; }
    public SpellInfo SpellInfo { get; }
    public Unit Victim { get; }

    public void AbsorbDamage(double amount)
    {
        amount = Math.Min(amount, Damage);
        Absorb += amount;
        Damage -= amount;
        HitMask |= ProcFlagsHit.Absorb;
    }

    public void ModifyDamage(double amount)
    {
        amount = Math.Max(amount, -Damage);
        Damage += amount;
    }

    public void ResistDamage(double amount)
    {
        amount = Math.Min(amount, Damage);
        Resist += amount;
        Damage -= amount;

        if (Damage != 0)
            return;

        HitMask |= ProcFlagsHit.FullResist;
        HitMask &= ~(ProcFlagsHit.Normal | ProcFlagsHit.Critical);
    }

    private void BlockDamage(double amount)
    {
        amount = Math.Min(amount, Damage);
        Block += amount;
        Damage -= amount;
        HitMask |= ProcFlagsHit.Block;

        if (Damage != 0)
            return;

        HitMask |= ProcFlagsHit.FullBlock;
        HitMask &= ~(ProcFlagsHit.Normal | ProcFlagsHit.Critical);
    }
}