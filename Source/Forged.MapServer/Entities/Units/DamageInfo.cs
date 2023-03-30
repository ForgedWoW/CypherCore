// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

public class DamageInfo
{
    public Unit Attacker { get; }

    public Unit Victim { get; }

    public SpellInfo SpellInfo { get; }

    public SpellSchoolMask SchoolMask { get; }

    public DamageEffectType DamageType { get; }

    public WeaponAttackType AttackType { get; }

    public double Damage { get; private set; }

    public double OriginalDamage { get; }

    public double Absorb { get; private set; }

    public double Resist { get; private set; }

    public double Block { get; private set; }

    public ProcFlagsHit HitMask { get; private set; }

    public bool IsImmune
    {
        get { return HitMask.HasFlag(ProcFlagsHit.Immune); }
    }

    public bool IsFullBlock
    {
        get { return HitMask.HasFlag(ProcFlagsHit.FullBlock); }
    }

    public bool IsBlock
    {
        get { return HitMask.HasFlag(ProcFlagsHit.Block); }
    }

    public bool IsAbsorb
    {
        get { return HitMask.HasFlag(ProcFlagsHit.Absorb); }
    }

    public bool IsFillResist
    {
        get { return HitMask.HasFlag(ProcFlagsHit.FullResist); }
    }

    public bool IsMiss
    {
        get { return HitMask.HasFlag(ProcFlagsHit.Miss); }
    }

    public bool IsDodge
    {
        get { return HitMask.HasFlag(ProcFlagsHit.Dodge); }
    }

    public bool IsParry
    {
        get { return HitMask.HasFlag(ProcFlagsHit.Parry); }
    }

    public bool IsEvade
    {
        get { return HitMask.HasFlag(ProcFlagsHit.Evade); }
    }

    public bool IsNormal
    {
        get { return HitMask.HasFlag(ProcFlagsHit.Normal); }
    }

    public bool IsCritical
    {
        get { return HitMask.HasFlag(ProcFlagsHit.Critical); }
    }

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

    public void ModifyDamage(double amount)
    {
        amount = Math.Max(amount, -Damage);
        Damage += amount;
    }

    public void AbsorbDamage(double amount)
    {
        amount = Math.Min(amount, Damage);
        Absorb += amount;
        Damage -= amount;
        HitMask |= ProcFlagsHit.Absorb;
    }

    public void ResistDamage(double amount)
    {
        amount = Math.Min(amount, Damage);
        Resist += amount;
        Damage -= amount;

        if (Damage == 0)
        {
            HitMask |= ProcFlagsHit.FullResist;
            HitMask &= ~(ProcFlagsHit.Normal | ProcFlagsHit.Critical);
        }
    }

    private void BlockDamage(double amount)
    {
        amount = Math.Min(amount, Damage);
        Block += amount;
        Damage -= amount;
        HitMask |= ProcFlagsHit.Block;

        if (Damage == 0)
        {
            HitMask |= ProcFlagsHit.FullBlock;
            HitMask &= ~(ProcFlagsHit.Normal | ProcFlagsHit.Critical);
        }
    }
}