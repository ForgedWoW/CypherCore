// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Maps;

public class AnyDeadUnitCheck : ICheck<Unit>
{
	public bool Invoke(Unit u)
	{
		return !u.IsAlive;
	}
}