// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

class NearestGameObjectFishingHole : ICheck<GameObject>
{
	readonly WorldObject _obj;
	float _range;

	public NearestGameObjectFishingHole(WorldObject obj, float range)
	{
		_obj   = obj;
		_range = range;
	}

	public bool Invoke(GameObject go)
	{
		if (go.GetGoInfo().type == GameObjectTypes.FishingHole && go.IsSpawned() && _obj.IsWithinDist(go, _range) && _obj.IsWithinDist(go, go.GetGoInfo().FishingHole.radius))
		{
			_range = _obj.GetDistance(go);

			return true;
		}

		return false;
	}
}