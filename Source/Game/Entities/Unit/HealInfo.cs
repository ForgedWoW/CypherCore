// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Spells;

namespace Game.Entities;

public class HealInfo
{
	readonly Unit _healer;
	readonly Unit _target;
	readonly double _originalHeal;
	readonly SpellInfo _spellInfo;
	readonly SpellSchoolMask _schoolMask;
	double _heal;
	double _effectiveHeal;
	double _absorb;
	ProcFlagsHit _hitMask;

	public HealInfo(Unit healer, Unit target, double heal, SpellInfo spellInfo, SpellSchoolMask schoolMask)
	{
		_healer = healer;
		_target = target;
		_heal = heal;
		_originalHeal = heal;
		_spellInfo = spellInfo;
		_schoolMask = schoolMask;
	}

	public void AbsorbHeal(double amount)
	{
		amount = Math.Min(amount, GetHeal());
		_absorb += amount;
		_heal -= amount;
		amount = Math.Min(amount, GetEffectiveHeal());
		_effectiveHeal -= amount;
		_hitMask |= ProcFlagsHit.Absorb;
	}

	public void SetEffectiveHeal(uint amount)
	{
		_effectiveHeal = amount;
	}

	public Unit GetHealer()
	{
		return _healer;
	}

	public Unit GetTarget()
	{
		return _target;
	}

	public double GetHeal()
	{
		return _heal;
	}

	public double GetOriginalHeal()
	{
		return _originalHeal;
	}

	public double GetEffectiveHeal()
	{
		return _effectiveHeal;
	}

	public double GetAbsorb()
	{
		return _absorb;
	}

	public SpellInfo GetSpellInfo()
	{
		return _spellInfo;
	}

	public SpellSchoolMask GetSchoolMask()
	{
		return _schoolMask;
	}

	ProcFlagsHit GetHitMask()
	{
		return _hitMask;
	}
}