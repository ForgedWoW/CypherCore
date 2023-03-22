// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

class NearestCreatureEntryWithLiveStateInObjectRangeCheck : ICheck<Creature>
{
	readonly WorldObject _obj;
	readonly uint _entry;
	readonly bool _alive;
	float _range;

	public NearestCreatureEntryWithLiveStateInObjectRangeCheck(WorldObject obj, uint entry, bool alive, float range)
	{
		_obj = obj;
		_entry = entry;
		_alive = alive;
		_range = range;
	}

	public bool Invoke(Creature u)
	{
		if (u.DeathState != DeathState.Dead && u.Entry == _entry && u.IsAlive == _alive && u.GUID != _obj.GUID && _obj.IsWithinDist(u, _range) && u.CheckPrivateObjectOwnerVisibility(_obj))
		{
			_range = _obj.GetDistance(u); // use found unit range as new range limit for next check

			return true;
		}

		return false;
	}
}