// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Spells;

namespace Game.Entities;

public class ProcEventInfo
{
	readonly Unit _actor;
	readonly Unit _actionTarget;
	readonly Unit _procTarget;
	readonly ProcFlagsInit _typeMask;
	readonly ProcFlagsSpellType _spellTypeMask;
	readonly ProcFlagsSpellPhase _spellPhaseMask;
	readonly ProcFlagsHit _hitMask;
	readonly Spell _spell;
	readonly DamageInfo _damageInfo;
	readonly HealInfo _healInfo;

	public ProcEventInfo(Unit actor, Unit actionTarget, Unit procTarget, ProcFlagsInit typeMask, ProcFlagsSpellType spellTypeMask,
						ProcFlagsSpellPhase spellPhaseMask, ProcFlagsHit hitMask, Spell spell, DamageInfo damageInfo, HealInfo healInfo)
	{
		_actor = actor;
		_actionTarget = actionTarget;
		_procTarget = procTarget;
		_typeMask = typeMask;
		_spellTypeMask = spellTypeMask;
		_spellPhaseMask = spellPhaseMask;
		_hitMask = hitMask;
		_spell = spell;
		_damageInfo = damageInfo;
		_healInfo = healInfo;
	}

	public Unit GetActor()
	{
		return _actor;
	}

	public Unit GetActionTarget()
	{
		return _actionTarget;
	}

	public Unit GetProcTarget()
	{
		return _procTarget;
	}

	public ProcFlagsInit GetTypeMask()
	{
		return _typeMask;
	}

	public ProcFlagsSpellType GetSpellTypeMask()
	{
		return _spellTypeMask;
	}

	public ProcFlagsSpellPhase GetSpellPhaseMask()
	{
		return _spellPhaseMask;
	}

	public ProcFlagsHit GetHitMask()
	{
		return _hitMask;
	}

	public SpellInfo GetSpellInfo()
	{
		if (_spell)
			return _spell.SpellInfo;

		if (_damageInfo != null)
			return _damageInfo.GetSpellInfo();

		if (_healInfo != null)
			return _healInfo.GetSpellInfo();

		return null;
	}

	public SpellSchoolMask GetSchoolMask()
	{
		if (_spell)
			return _spell.SpellInfo.GetSchoolMask();

		if (_damageInfo != null)
			return _damageInfo.GetSchoolMask();

		if (_healInfo != null)
			return _healInfo.GetSchoolMask();

		return SpellSchoolMask.None;
	}

	public DamageInfo GetDamageInfo()
	{
		return _damageInfo;
	}

	public HealInfo GetHealInfo()
	{
		return _healInfo;
	}

	public Spell GetProcSpell()
	{
		return _spell;
	}
}