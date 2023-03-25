// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.GridNotifiers;

class MostHPPercentMissingInRange : ICheck<Unit>
{
	readonly Unit _obj;
	readonly float _range;
	readonly float _minHpPct;
	readonly float _maxHpPct;
	float _hpPct;

	public MostHPPercentMissingInRange(Unit obj, float range, uint minHpPct, uint maxHpPct)
	{
		_obj = obj;
		_range = range;
		_minHpPct = minHpPct;
		_maxHpPct = maxHpPct;
		_hpPct = 101.0f;
	}

	public bool Invoke(Unit u)
	{
		if (u.IsAlive && u.IsInCombat && !_obj.IsHostileTo(u) && _obj.IsWithinDist(u, _range) && _minHpPct <= u.HealthPct && u.HealthPct <= _maxHpPct && u.HealthPct < _hpPct)
		{
			_hpPct = u.HealthPct;

			return true;
		}

		return false;
	}
}