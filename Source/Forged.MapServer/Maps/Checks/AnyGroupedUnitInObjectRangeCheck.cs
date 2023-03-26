// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.Checks;

public class AnyGroupedUnitInObjectRangeCheck : ICheck<Unit>
{
    private readonly WorldObject _source;
    private readonly Unit _refUnit;
    private readonly float _range;
    private readonly bool _raid;
    private readonly bool _playerOnly;
    private readonly bool _incOwnRadius;
    private readonly bool _incTargetRadius;

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
		if (_playerOnly && !u.IsPlayer)
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

		if (!u.IsAlive)
			return false;

		var searchRadius = _range;

		if (_incOwnRadius)
			searchRadius += _source.CombatReach;

		if (_incTargetRadius)
			searchRadius += u.CombatReach;

		return u.IsInMap(_source) && u.InSamePhase(_source) && u.Location.IsWithinDoubleVerticalCylinder(_source.Location, searchRadius, searchRadius);
	}
}