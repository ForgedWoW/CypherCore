// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

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
		if (go.GetGoType() == _type && _obj.IsWithinDist(go, _range))
		{
			_range = _obj.GetDistance(go); // use found GO range as new range limit for next check

			return true;
		}

		return false;
	}
}