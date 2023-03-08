// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class AnyGroupedUnitInObjectRangeCheck : ICheck<Unit>
{
	readonly WorldObject _source;
	readonly Unit _refUnit;
	readonly float _range;
	readonly bool _raid;
	readonly bool _playerOnly;
	readonly bool _incOwnRadius;
	readonly bool _incTargetRadius;

	public AnyGroupedUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, bool raid, bool playerOnly = false, bool incOwnRadius = true, bool incTargetRadius = true)
	{
		_source = obj;
		_refUnit = funit;
		_range = range;
		_raid = raid;
		_playerOnly = playerOnly;
		_incOwnRadius = incOwnRadius;
		_incTargetRadius = incTargetRadius;
	}

	public bool Invoke(Unit u)
	{
		if (_playerOnly && !u.IsPlayer())
			return false;

		if (_raid)
		{
			if (!_refUnit.IsInRaidWith(u))
				return false;
		}
		else if (!_refUnit.IsInPartyWith(u))
		{
			return false;
		}

		if (_refUnit.IsHostileTo(u))
			return false;

		if (!u.IsAlive())
			return false;

		var searchRadius = _range;

		if (_incOwnRadius)
			searchRadius += _source.GetCombatReach();

		if (_incTargetRadius)
			searchRadius += u.GetCombatReach();

		return u.IsInMap(_source) && u.InSamePhase(_source) && u.Location.IsWithinDoubleVerticalCylinder(_source.Location, searchRadius, searchRadius);
	}
}