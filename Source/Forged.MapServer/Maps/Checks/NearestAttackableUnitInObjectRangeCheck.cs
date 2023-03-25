// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.Checks;

public class NearestAttackableUnitInObjectRangeCheck : ICheck<Unit>
{
	readonly WorldObject _obj;
	readonly Unit _funit;
	float _range;

	public NearestAttackableUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range)
	{
		_obj = obj;
		_funit = funit;
		_range = range;
	}

	public bool Invoke(Unit u)
	{
		if (u.IsTargetableForAttack() &&
			_obj.IsWithinDist(u, _range) &&
			(_funit.IsInCombatWith(u) || _funit.IsHostileTo(u)) &&
			_obj.CanSeeOrDetect(u))
		{
			_range = _obj.GetDistance(u); // use found unit range as new range limit for next check

			return true;
		}

		return false;
	}
}