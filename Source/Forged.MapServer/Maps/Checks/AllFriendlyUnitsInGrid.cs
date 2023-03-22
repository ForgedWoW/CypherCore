// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class AllFriendlyUnitsInGrid : ICheck<Unit>
{
	readonly Unit _unit;

	public AllFriendlyUnitsInGrid(Unit obj)
	{
		_unit = obj;
	}

	public bool Invoke(Unit u)
	{
		if (u.IsAlive && u.IsVisible() && u.IsFriendlyTo(_unit))
			return true;

		return false;
	}
}