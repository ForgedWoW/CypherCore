// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class AnyUnfriendlyUnitInObjectRangeCheck : ICheck<Unit>
{
	readonly WorldObject _obj;
	readonly Unit _funit;
	readonly float _range;
	readonly Func<Unit, bool> _additionalCheck;

	public AnyUnfriendlyUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, Func<Unit, bool> additionalCheck = null)
	{
		_obj = obj;
		_funit = funit;
		_range = range;
		_additionalCheck = additionalCheck;
	}

	public bool Invoke(Unit u)
	{
		if (u.IsAlive() && _obj.IsWithinDist(u, _range) && !_funit.IsFriendlyTo(u) && (_additionalCheck == null || _additionalCheck.Invoke(u)))
			return true;
		else
			return false;
	}
}