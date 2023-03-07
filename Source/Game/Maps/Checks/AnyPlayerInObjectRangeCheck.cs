// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class AnyPlayerInObjectRangeCheck : ICheck<Player>
{
	readonly WorldObject _obj;
	readonly float _range;
	readonly bool _reqAlive;

	public AnyPlayerInObjectRangeCheck(WorldObject obj, float range, bool reqAlive = true)
	{
		_obj      = obj;
		_range    = range;
		_reqAlive = reqAlive;
	}

	public bool Invoke(Player pl)
	{
		if (_reqAlive && !pl.IsAlive())
			return false;

		if (!_obj.IsWithinDist(pl, _range))
			return false;

		return true;
	}
}