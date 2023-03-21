// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Maps;

class AnyPlayerInPositionRangeCheck : ICheck<Player>
{
	readonly Position _pos;
	readonly float _range;
	readonly bool _reqAlive;

	public AnyPlayerInPositionRangeCheck(Position pos, float range, bool reqAlive = true)
	{
		_pos = pos;
		_range = range;
		_reqAlive = reqAlive;
	}

	public bool Invoke(Player u)
	{
		if (_reqAlive && !u.IsAlive)
			return false;

		if (!u.IsWithinDist3d(_pos, _range))
			return false;

		return true;
	}
}