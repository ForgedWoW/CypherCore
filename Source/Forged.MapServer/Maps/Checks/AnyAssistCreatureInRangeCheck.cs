// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.Checks;

class AnyAssistCreatureInRangeCheck : ICheck<Creature>
{
	readonly Unit _funit;
	readonly Unit _enemy;
	readonly float _range;

	public AnyAssistCreatureInRangeCheck(Unit funit, Unit enemy, float range)
	{
		_funit = funit;
		_enemy = enemy;
		_range = range;
	}

	public bool Invoke(Creature u)
	{
		if (u == _funit)
			return false;

		if (!u.CanAssistTo(_funit, _enemy))
			return false;

		// too far
		// Don't use combat reach distance, range must be an absolute value, otherwise the chain aggro range will be too big
		if (!_funit.IsWithinDist(u, _range, true, false, false))
			return false;

		// only if see assisted creature
		if (!_funit.IsWithinLOSInMap(u))
			return false;

		return true;
	}
}