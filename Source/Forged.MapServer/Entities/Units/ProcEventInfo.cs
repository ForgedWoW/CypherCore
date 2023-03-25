// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

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

	public Unit Actor => _actor;

	public Unit ActionTarget => _actionTarget;

	public Unit ProcTarget => _procTarget;

	public ProcFlagsInit TypeMask => _typeMask;

	public ProcFlagsSpellType SpellTypeMask => _spellTypeMask;

	public ProcFlagsSpellPhase SpellPhaseMask => _spellPhaseMask;

	public ProcFlagsHit HitMask => _hitMask;

	public SpellInfo SpellInfo
	{
		get
		{
			if (_spell)
				return _spell.SpellInfo;

			if (_damageInfo != null)
				return _damageInfo.SpellInfo;

			if (_healInfo != null)
				return _healInfo.SpellInfo;

			return null;
		}
	}

	public SpellSchoolMask SchoolMask
	{
		get
		{
			if (_spell)
				return _spell.SpellInfo.GetSchoolMask();

			if (_damageInfo != null)
				return _damageInfo.SchoolMask;

			if (_healInfo != null)
				return _healInfo.SchoolMask;

			return SpellSchoolMask.None;
		}
	}

	public DamageInfo DamageInfo => _damageInfo;

	public HealInfo HealInfo => _healInfo;

	public Spell ProcSpell => _spell;

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
}