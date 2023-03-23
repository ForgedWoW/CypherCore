// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Game.Common.Entities.Objects;
using Game.Entities;

namespace Game.Common.Entities.Units;

public class HealthPctOrderPred : IComparer<WorldObject>
{
	readonly bool _ascending;

	public HealthPctOrderPred(bool ascending = true)
	{
		_ascending = ascending;
	}

	public int Compare(WorldObject objA, WorldObject objB)
	{
		var a = objA.AsUnit;
		var b = objB.AsUnit;
		var rA = a.MaxHealth != 0 ? a.Health / (float)a.MaxHealth : 0.0f;
		var rB = b.MaxHealth != 0 ? b.Health / (float)b.MaxHealth : 0.0f;

		return Convert.ToInt32(_ascending ? rA < rB : rA > rB);
	}
}
