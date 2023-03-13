// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Spells;

namespace Game.Entities;

public class DamageInfo
{
	readonly Unit _attacker;
	readonly Unit _victim;
	readonly double _originalDamage;
	readonly SpellInfo _spellInfo;
	readonly SpellSchoolMask _schoolMask;
	readonly DamageEffectType _damageType;
	readonly WeaponAttackType _attackType;
	double _damage;
	double _absorb;
	double _resist;
	double _block;
	ProcFlagsHit _hitMask;

    public Unit Attacker => _attacker;
    public Unit Victim => _victim;
    public SpellInfo SpellInfo => _spellInfo;
    public SpellSchoolMask SchoolMask => _schoolMask;
    public DamageEffectType DamageType => _damageType;
    public WeaponAttackType AttackType => _attackType;
    public double Damage => _damage;
    public double OriginalDamage => _originalDamage;
    public double Absorb => _absorb;
    public double Resist => _resist;
    public double Block => _block;
    public ProcFlagsHit HitMask => _hitMask;
	public bool IsImmune { get { return _hitMask.HasFlag(ProcFlagsHit.Immune); } }
    public bool IsFullBlock { get { return _hitMask.HasFlag(ProcFlagsHit.FullBlock); } }
    public bool IsBlock { get { return _hitMask.HasFlag(ProcFlagsHit.Block); } }
    public bool IsAbsorb { get { return _hitMask.HasFlag(ProcFlagsHit.Absorb); } }
    public bool IsFillResist { get { return _hitMask.HasFlag(ProcFlagsHit.FullResist); } }
    public bool IsMiss { get { return _hitMask.HasFlag(ProcFlagsHit.Miss); } }
    public bool IsDodge { get { return _hitMask.HasFlag(ProcFlagsHit.Dodge); } }
    public bool IsParry { get { return _hitMask.HasFlag(ProcFlagsHit.Parry); } }
    public bool IsEvade { get { return _hitMask.HasFlag(ProcFlagsHit.Evade); } }
    public bool IsNormal { get { return _hitMask.HasFlag(ProcFlagsHit.Normal); } }
    public bool IsCritical { get { return _hitMask.HasFlag(ProcFlagsHit.Critical); } }

    public DamageInfo(Unit attacker, Unit victim, double damage, SpellInfo spellInfo, SpellSchoolMask schoolMask, DamageEffectType damageType, WeaponAttackType attackType)
	{
		_attacker = attacker;
		_victim = victim;
		_damage = damage;
		_originalDamage = damage;
		_spellInfo = spellInfo;
		_schoolMask = schoolMask;
		_damageType = damageType;
		_attackType = attackType;
	}

	public DamageInfo(CalcDamageInfo dmgInfo)
	{
		_attacker = dmgInfo.Attacker;
		_victim = dmgInfo.Target;
		_damage = dmgInfo.Damage;
		_originalDamage = dmgInfo.Damage;
		_spellInfo = null;
		_schoolMask = (SpellSchoolMask)dmgInfo.DamageSchoolMask;
		_damageType = DamageEffectType.Direct;
		_attackType = dmgInfo.AttackType;
		_absorb = dmgInfo.Absorb;
		_resist = dmgInfo.Resist;
		_block = dmgInfo.Blocked;

		switch (dmgInfo.TargetState)
		{
			case VictimState.Immune:
				_hitMask |= ProcFlagsHit.Immune;

				break;
			case VictimState.Blocks:
				_hitMask |= ProcFlagsHit.FullBlock;

				break;
		}

		if (dmgInfo.HitInfo.HasAnyFlag(HitInfo.PartialAbsorb | HitInfo.FullAbsorb))
			_hitMask |= ProcFlagsHit.Absorb;

		if (dmgInfo.HitInfo.HasAnyFlag(HitInfo.FullResist))
			_hitMask |= ProcFlagsHit.FullResist;

		if (_block != 0)
			_hitMask |= ProcFlagsHit.Block;

		var damageNullified = dmgInfo.HitInfo.HasAnyFlag(HitInfo.FullAbsorb | HitInfo.FullResist) || _hitMask.HasAnyFlag(ProcFlagsHit.Immune | ProcFlagsHit.FullBlock);

		switch (dmgInfo.HitOutCome)
		{
			case MeleeHitOutcome.Miss:
				_hitMask |= ProcFlagsHit.Miss;

				break;
			case MeleeHitOutcome.Dodge:
				_hitMask |= ProcFlagsHit.Dodge;

				break;
			case MeleeHitOutcome.Parry:
				_hitMask |= ProcFlagsHit.Parry;

				break;
			case MeleeHitOutcome.Evade:
				_hitMask |= ProcFlagsHit.Evade;

				break;
			case MeleeHitOutcome.Block:
			case MeleeHitOutcome.Crushing:
			case MeleeHitOutcome.Glancing:
			case MeleeHitOutcome.Normal:
				if (!damageNullified)
					_hitMask |= ProcFlagsHit.Normal;

				break;
			case MeleeHitOutcome.Crit:
				if (!damageNullified)
					_hitMask |= ProcFlagsHit.Critical;

				break;
		}
	}

	public DamageInfo(SpellNonMeleeDamage spellNonMeleeDamage, DamageEffectType damageType, WeaponAttackType attackType, ProcFlagsHit hitMask)
	{
		_attacker = spellNonMeleeDamage.Attacker;
		_victim = spellNonMeleeDamage.Target;
		_damage = spellNonMeleeDamage.Damage;
		_spellInfo = spellNonMeleeDamage.Spell;
		_schoolMask = spellNonMeleeDamage.SchoolMask;
		_damageType = damageType;
		_attackType = attackType;
		_absorb = spellNonMeleeDamage.Absorb;
		_resist = spellNonMeleeDamage.Resist;
		_block = spellNonMeleeDamage.Blocked;
		_hitMask = hitMask;

		if (spellNonMeleeDamage.Blocked != 0)
			_hitMask |= ProcFlagsHit.Block;

		if (spellNonMeleeDamage.Absorb != 0)
			_hitMask |= ProcFlagsHit.Absorb;
	}

	public void ModifyDamage(double amount)
	{
		amount = Math.Max(amount, -Damage);
		_damage += amount;
	}

	public void AbsorbDamage(double amount)
	{
		amount = Math.Min(amount, Damage);
		_absorb += amount;
		_damage -= amount;
		_hitMask |= ProcFlagsHit.Absorb;
	}

	public void ResistDamage(double amount)
	{
		amount = Math.Min(amount, Damage);
		_resist += amount;
		_damage -= amount;

		if (_damage == 0)
		{
			_hitMask |= ProcFlagsHit.FullResist;
			_hitMask &= ~(ProcFlagsHit.Normal | ProcFlagsHit.Critical);
		}
	}

	void BlockDamage(double amount)
	{
		amount = Math.Min(amount, Damage);
		_block += amount;
		_damage -= amount;
		_hitMask |= ProcFlagsHit.Block;

		if (_damage == 0)
		{
			_hitMask |= ProcFlagsHit.FullBlock;
			_hitMask &= ~(ProcFlagsHit.Normal | ProcFlagsHit.Critical);
		}
	}
}