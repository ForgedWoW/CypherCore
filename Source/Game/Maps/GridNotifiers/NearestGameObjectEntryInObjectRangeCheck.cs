// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class NearestGameObjectEntryInObjectRangeCheck : ICheck<GameObject>
{
	readonly WorldObject _obj;
	readonly uint _entry;
	readonly bool _spawnedOnly;
	float _range;

	public NearestGameObjectEntryInObjectRangeCheck(WorldObject obj, uint entry, float range, bool spawnedOnly = true)
	{
		_obj = obj;
		_entry = entry;
		_range = range;
		_spawnedOnly = spawnedOnly;
	}

	public bool Invoke(GameObject go)
	{
		if ((!_spawnedOnly || go.IsSpawned()) && go.GetEntry() == _entry && go.GetGUID() != _obj.GetGUID() && _obj.IsWithinDist(go, _range))
		{
			_range = _obj.GetDistance(go); // use found GO range as new range limit for next check

			return true;
		}

		return false;
	}
}