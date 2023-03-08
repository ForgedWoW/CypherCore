// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

public class FriendlyCCedInRange : ICheck<Creature>
{
	readonly Unit _obj;
	readonly float _range;

	public FriendlyCCedInRange(Unit obj, float range)
	{
		_obj = obj;
		_range = range;
	}

	public bool Invoke(Creature u)
	{
		if (u.IsAlive &&
			u.			IsInCombat &&
			!_obj.IsHostileTo(u) &&
			_obj.IsWithinDist(u, _range) &&
			(u.IsFeared || u.IsCharmed || u.HasRootAura || u.HasUnitState(UnitState.Stunned) || u.HasUnitState(UnitState.Confused)))
			return true;

		return false;
	}
}