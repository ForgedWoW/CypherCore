// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class MostHPMissingInRange<T> : ICheck<T> where T : Unit
{
	readonly Unit _obj;
	readonly float _range;
	long _hp;

	public MostHPMissingInRange(Unit obj, float range, uint hp)
	{
		_obj = obj;
		_range = range;
		_hp = hp;
	}

	public bool Invoke(T u)
	{
		if (u.IsAlive && u.IsInCombat && !_obj.IsHostileTo(u) && _obj.IsWithinDist(u, _range) && u.GetMaxHealth() - u.GetHealth() > _hp)
		{
			_hp = (uint)(u.GetMaxHealth() - u.GetHealth());

			return true;
		}

		return false;
	}
}