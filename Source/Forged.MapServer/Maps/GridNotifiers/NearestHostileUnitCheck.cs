// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

public class NearestHostileUnitCheck : ICheck<Unit>
{
	readonly Creature _me;
	readonly bool _playerOnly;
	float _range;

	public NearestHostileUnitCheck(Creature creature, float dist = 0, bool playerOnly = false)
	{
		_me = creature;
		_playerOnly = playerOnly;

		_range = (dist == 0 ? 9999 : dist);
	}

	public bool Invoke(Unit u)
	{
		if (!_me.IsWithinDist(u, _range))
			return false;

		if (!_me.IsValidAttackTarget(u))
			return false;

		if (_playerOnly && !u.IsTypeId(TypeId.Player))
			return false;

		_range = _me.GetDistance(u); // use found unit range as new range limit for next check

		return true;
	}
}