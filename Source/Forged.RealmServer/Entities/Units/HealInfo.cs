// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Forged.RealmServer.Spells;

namespace Forged.RealmServer.Entities;

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

	public Unit Healer => _healer;
	public Unit Target => _target;
	public double OriginalHeal => _originalHeal;
	public SpellInfo SpellInfo => _spellInfo;
	public SpellSchoolMask SchoolMask => _schoolMask;
	public double Heal => _heal;
	public double EffectiveHeal => _effectiveHeal;
	public double Absorb => _absorb;
	public ProcFlagsHit HitMask => _hitMask;

	public bool IsCritical
	{
		get { return _hitMask.HasFlag(ProcFlagsHit.Critical); }
	}

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
		amount = Math.Min(amount, Heal);
		_absorb += amount;
		_heal -= amount;
		amount = Math.Min(amount, EffectiveHeal);
		_effectiveHeal -= amount;
		_hitMask |= ProcFlagsHit.Absorb;
	}

	public void SetEffectiveHeal(uint amount)
	{
		_effectiveHeal = amount;
	}
}