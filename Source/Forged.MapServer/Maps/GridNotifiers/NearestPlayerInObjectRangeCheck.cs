// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Maps.GridNotifiers;

internal class NearestPlayerInObjectRangeCheck : ICheck<Player>
{
    private readonly WorldObject _obj;
    private float _range;

	public NearestPlayerInObjectRangeCheck(WorldObject obj, float range)
	{
		_obj = obj;
		_range = range;
	}

	public bool Invoke(Player pl)
	{
		if (pl.IsAlive && _obj.IsWithinDist(pl, _range))
		{
			_range = _obj.GetDistance(pl);

			return true;
		}

		return false;
	}
}