// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.Checks;

internal class AllFriendlyUnitsInGrid : ICheck<Unit>
{
    private readonly Unit _unit;

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