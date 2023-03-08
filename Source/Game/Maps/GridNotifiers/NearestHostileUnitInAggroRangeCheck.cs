// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class NearestHostileUnitInAggroRangeCheck : ICheck<Unit>
{
	readonly Creature _me;
	readonly bool _useLOS;
	readonly bool _ignoreCivilians;

	public NearestHostileUnitInAggroRangeCheck(Creature creature, bool useLOS = false, bool ignoreCivilians = false)
	{
		_me = creature;
		_useLOS = useLOS;
		_ignoreCivilians = ignoreCivilians;
	}

	public bool Invoke(Unit u)
	{
		if (!u.IsHostileTo(_me))
			return false;

		if (!u.IsWithinDist(_me, _me.GetAggroRange(u)))
			return false;

		if (!_me.IsValidAttackTarget(u))
			return false;

		if (_useLOS && !u.IsWithinLOSInMap(_me))
			return false;

		// pets in aggressive do not attack civilians
		if (_ignoreCivilians)
		{
			var c = u.ToCreature();

			if (c != null)
				if (c.IsCivilian)
					return false;
		}

		return true;
	}
}