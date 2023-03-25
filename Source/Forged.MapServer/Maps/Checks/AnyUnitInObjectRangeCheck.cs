// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.Checks;

public class AnyUnitInObjectRangeCheck : ICheck<Unit>
{
	readonly WorldObject _obj;
	readonly float _range;
	readonly bool _check3D;

	public AnyUnitInObjectRangeCheck(WorldObject obj, float range, bool check3D = true)
	{
		_obj = obj;
		_range = range;
		_check3D = check3D;
	}

	public bool Invoke(Unit u)
	{
		if (u.IsAlive && _obj.IsWithinDist(u, _range, _check3D))
			return true;

		return false;
	}
}