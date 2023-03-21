// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Maps;

class PlayerAtMinimumRangeAway : ICheck<Player>
{
	readonly Unit _unit;
	readonly float _fRange;

	public PlayerAtMinimumRangeAway(Unit unit, float fMinRange)
	{
		_unit = unit;
		_fRange = fMinRange;
	}

	public bool Invoke(Player player)
	{
		//No threat list check, must be done explicit if expected to be in combat with creature
		if (!player.IsGameMaster && player.IsAlive && !_unit.IsWithinDist(player, _fRange, false))
			return true;

		return false;
	}
}