// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.AI;

public class SpellTargetSelector : ICheck<Unit>
{
	readonly Unit _caster;
	readonly SpellInfo _spellInfo;

	public SpellTargetSelector(Unit caster, uint spellId)
	{
		_caster = caster;
		_spellInfo = Global.SpellMgr.GetSpellInfo(spellId, caster.Map.DifficultyID);

		Cypher.Assert(_spellInfo != null);
	}

	public bool Invoke(Unit target)
	{
		if (target == null)
			return false;

		if (_spellInfo.CheckTarget(_caster, target) != SpellCastResult.SpellCastOk)
			return false;

		// copypasta from Spell.CheckRange
		var minRange = 0.0f;
		var maxRange = 0.0f;
		var rangeMod = 0.0f;

		if (_spellInfo.RangeEntry != null)
		{
			if (_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Melee))
			{
				rangeMod = _caster.CombatReach + 4.0f / 3.0f;
				rangeMod += target.CombatReach;

				rangeMod = Math.Max(rangeMod, SharedConst.NominalMeleeRange);
			}
			else
			{
				var meleeRange = 0.0f;

				if (_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
				{
					meleeRange = _caster.CombatReach + 4.0f / 3.0f;
					meleeRange += target.CombatReach;

					meleeRange = Math.Max(meleeRange, SharedConst.NominalMeleeRange);
				}

				minRange = _caster.GetSpellMinRangeForTarget(target, _spellInfo) + meleeRange;
				maxRange = _caster.GetSpellMaxRangeForTarget(target, _spellInfo);

				rangeMod = _caster.CombatReach;
				rangeMod += target.CombatReach;

				if (minRange > 0.0f && !_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
					minRange += rangeMod;
			}

			if (_caster.IsMoving &&
				target.IsMoving &&
				!_caster.IsWalking &&
				!target.IsWalking &&
				(_spellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Melee) || target.IsTypeId(TypeId.Player)))
				rangeMod += 8.0f / 3.0f;
		}

		maxRange += rangeMod;

		minRange *= minRange;
		maxRange *= maxRange;

		if (target != _caster)
		{
			if (_caster.Location.GetExactDistSq(target.Location) > maxRange)
				return false;

			if (minRange > 0.0f && _caster.Location.GetExactDistSq(target.Location) < minRange)
				return false;
		}

		return true;
	}
}