// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Units;

namespace Forged.RealmServer.AI;

class PowerUsersSelector : ICheck<Unit>
{
	readonly Unit _me;
	readonly PowerType _power;
	readonly float _dist;
	readonly bool _playerOnly;

	public PowerUsersSelector(Unit unit, PowerType power, float dist, bool playerOnly)
	{
		_me = unit;
		_power = power;
		_dist = dist;
		_playerOnly = playerOnly;
	}

	public bool Invoke(Unit target)
	{
		if (_me == null || target == null)
			return false;

		if (target.DisplayPowerType != _power)
			return false;

		if (_playerOnly && target.TypeId != TypeId.Player)
			return false;

		if (_dist > 0.0f && !_me.IsWithinCombatRange(target, _dist))
			return false;

		if (_dist < 0.0f && _me.IsWithinCombatRange(target, -_dist))
			return false;

		return true;
	}
}