// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;

namespace Game.Entities;

public class HealthPctOrderPred : IComparer<WorldObject>
{
	readonly bool _ascending;

	public HealthPctOrderPred(bool ascending = true)
	{
		_ascending = ascending;
	}

	public int Compare(WorldObject objA, WorldObject objB)
	{
		var a = objA.ToUnit();
		var b = objB.ToUnit();
		var rA = a.GetMaxHealth() != 0 ? a.GetHealth() / (float)a.GetMaxHealth() : 0.0f;
		var rB = b.GetMaxHealth() != 0 ? b.GetHealth() / (float)b.GetMaxHealth() : 0.0f;

		return Convert.ToInt32(_ascending ? rA < rB : rA > rB);
	}
}