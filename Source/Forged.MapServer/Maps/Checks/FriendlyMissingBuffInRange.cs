// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class FriendlyMissingBuffInRange : ICheck<Creature>
{
	readonly Unit _obj;
	readonly float _range;
	readonly uint _spell;

	public FriendlyMissingBuffInRange(Unit obj, float range, uint spellid)
	{
		_obj = obj;
		_range = range;
		_spell = spellid;
	}

	public bool Invoke(Creature u)
	{
		if (u.IsAlive &&
			u.IsInCombat &&
			!_obj.IsHostileTo(u) &&
			_obj.IsWithinDist(u, _range) &&
			!(u.HasAura(_spell)))
			return true;

		return false;
	}
}