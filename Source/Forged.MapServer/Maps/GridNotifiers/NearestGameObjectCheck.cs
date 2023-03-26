// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps.GridNotifiers;

internal class NearestGameObjectCheck : ICheck<GameObject>
{
    private readonly WorldObject _obj;
    private float _range;

	public NearestGameObjectCheck(WorldObject obj)
	{
		_obj = obj;
		_range = 999;
	}

	public bool Invoke(GameObject go)
	{
		if (_obj.IsWithinDist(go, _range))
		{
			_range = _obj.GetDistance(go); // use found GO range as new range limit for next check

			return true;
		}

		return false;
	}
}