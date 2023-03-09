// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.AI;

// default predicate function to select target based on distance, player and/or aura criteria
public class DefaultTargetSelector : ICheck<Unit>
{
	readonly Unit _me;
	readonly float _dist;
	readonly bool _playerOnly;
	readonly Unit _exception;
	readonly int _aura;

    /// <param name="unit"> the reference unit </param>
    /// <param name="dist"> if 0: ignored, if > 0: maximum distance to the reference unit, if < 0: minimum distance to the reference unit </param>
    /// <param name="playerOnly"> self explaining </param>
    /// <param name="withTank"> allow current tank to be selected </param>
    /// <param name="aura"> if 0: ignored, if > 0: the target shall have the aura, if < 0, the target shall NOT have the aura </param>
    public DefaultTargetSelector(Unit unit, float dist, bool playerOnly, bool withTank, int aura)
	{
		_me = unit;
		_dist = dist;
		_playerOnly = playerOnly;
		_exception = !withTank ? unit.GetThreatManager().LastVictim : null;
		_aura = aura;
	}

	public bool Invoke(Unit target)
	{
		if (_me == null)
			return false;

		if (target == null)
			return false;

		if (_exception != null && target == _exception)
			return false;

		if (_playerOnly && !target.IsTypeId(TypeId.Player))
			return false;

		if (_dist > 0.0f && !_me.IsWithinCombatRange(target, _dist))
			return false;

		if (_dist < 0.0f && _me.IsWithinCombatRange(target, -_dist))
			return false;

		if (_aura != 0)
		{
			if (_aura > 0)
			{
				if (!target.HasAura((uint)_aura))
					return false;
			}
			else
			{
				if (target.HasAura((uint)-_aura))
					return false;
			}
		}

		return false;
	}
}

// Target selector for spell casts checking range, auras and attributes
// todo Add more checks from Spell.CheckCast

// Very simple target selector, will just skip main target
// NOTE: When passing to UnitAI.SelectTarget remember to use 0 as position for random selection
//       because tank will not be in the temporary list

// Simple selector for units using mana