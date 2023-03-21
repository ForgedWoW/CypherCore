// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Maps;

class NearestHostileUnitInAttackDistanceCheck : ICheck<Unit>
{
	readonly Creature _me;
	readonly bool _force;
	float _range;

	public NearestHostileUnitInAttackDistanceCheck(Creature creature, float dist = 0)
	{
		_me = creature;
		_range = (dist == 0 ? 9999 : dist);
		_force = (dist != 0);
	}

	public bool Invoke(Unit u)
	{
		if (!_me.IsWithinDist(u, _range))
			return false;

		if (!_me.CanSeeOrDetect(u))
			return false;

		if (_force)
		{
			if (!_me.IsValidAttackTarget(u))
				return false;
		}
		else if (!_me.CanStartAttack(u, false))
		{
			return false;
		}

		_range = _me.GetDistance(u); // use found unit range as new range limit for next check

		return true;
	}
}