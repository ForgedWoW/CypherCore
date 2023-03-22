﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class NearestUnspawnedGameObjectEntryInObjectRangeCheck : ICheck<GameObject>
{
	readonly WorldObject _obj;
	readonly uint _entry;
	float _range;

	public NearestUnspawnedGameObjectEntryInObjectRangeCheck(WorldObject obj, uint entry, float range)
	{
		_obj = obj;
		_entry = entry;
		_range = range;
	}

	public bool Invoke(GameObject go)
	{
		if (!go.IsSpawned && go.Entry == _entry && go.GUID != _obj.GUID && _obj.IsWithinDist(go, _range))
		{
			_range = _obj.GetDistance(go); // use found GO range as new range limit for next check

			return true;
		}

		return false;
	}
}