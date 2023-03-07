// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class GameObjectInRangeCheck : ICheck<GameObject>
{
	readonly float _x, _y, _z, _range;
	readonly uint _entry;

	public GameObjectInRangeCheck(float x, float y, float z, float range, uint entry = 0)
	{
		_x     = x;
		_y     = y;
		_z     = z;
		_range = range;
		_entry = entry;
	}

	public bool Invoke(GameObject go)
	{
		if (_entry == 0 || (go.GetGoInfo() != null && go.GetGoInfo().entry == _entry))
			return go.IsInRange(_x, _y, _z, _range);
		else return false;
	}
}