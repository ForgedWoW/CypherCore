// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class HeightDifferenceCheck : ICheck<WorldObject>
{
	readonly WorldObject _baseObject;
	readonly float _difference;
	readonly bool _reverse;

	public HeightDifferenceCheck(WorldObject go, float diff, bool reverse)
	{
		_baseObject = go;
		_difference = diff;
		_reverse    = reverse;
	}

	public bool Invoke(WorldObject unit)
	{
		return (unit.GetPositionZ() - _baseObject.GetPositionZ() > _difference) != _reverse;
	}
}