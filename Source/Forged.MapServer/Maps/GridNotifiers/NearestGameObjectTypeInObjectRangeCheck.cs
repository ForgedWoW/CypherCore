// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

class NearestGameObjectTypeInObjectRangeCheck : ICheck<GameObject>
{
	readonly WorldObject _obj;
	readonly GameObjectTypes _type;
	float _range;

	public NearestGameObjectTypeInObjectRangeCheck(WorldObject obj, GameObjectTypes type, float range)
	{
		_obj = obj;
		_type = type;
		_range = range;
	}

	public bool Invoke(GameObject go)
	{
		if (go.GoType == _type && _obj.IsWithinDist(go, _range))
		{
			_range = _obj.GetDistance(go); // use found GO range as new range limit for next check

			return true;
		}

		return false;
	}
}