// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

public class AnyFriendlyUnitInObjectRangeCheck : ICheck<Unit>
{
	readonly WorldObject _obj;
	readonly Unit _funit;
	readonly float _range;
	readonly bool _playerOnly;
	readonly bool _incOwnRadius;
	readonly bool _incTargetRadius;

	public AnyFriendlyUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, bool playerOnly = false, bool incOwnRadius = true, bool incTargetRadius = true)
	{
		_obj             = obj;
		_funit           = funit;
		_range           = range;
		_playerOnly      = playerOnly;
		_incOwnRadius    = incOwnRadius;
		_incTargetRadius = incTargetRadius;
	}

	public bool Invoke(Unit u)
	{
		if (!u.IsAlive())
			return false;

		var searchRadius = _range;

		if (_incOwnRadius)
			searchRadius += _obj.GetCombatReach();

		if (_incTargetRadius)
			searchRadius += u.GetCombatReach();

		if (!u.IsInMap(_obj) || !u.InSamePhase(_obj) || !u.IsWithinDoubleVerticalCylinder(_obj, searchRadius, searchRadius))
			return false;

		if (!_funit.IsFriendlyTo(u))
			return false;

		return !_playerOnly || u.GetTypeId() == TypeId.Player;
	}
}