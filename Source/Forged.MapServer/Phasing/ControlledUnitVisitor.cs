// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;

namespace Forged.MapServer.Phasing;

class ControlledUnitVisitor
{
	readonly HashSet<WorldObject> _visited = new();

	public ControlledUnitVisitor(WorldObject owner)
	{
		_visited.Add(owner);
	}

	public void VisitControlledOf(Unit unit, Action<Unit> func)
	{
		foreach (var controlled in unit.Controlled)
			// Player inside nested vehicle should not phase the root vehicle and its accessories (only direct root vehicle control does)
			if (!controlled.IsPlayer && controlled.Vehicle1 == null)
				if (_visited.Add(controlled))
					func(controlled);

		foreach (var summonGuid in unit.SummonSlot)
			if (!summonGuid.IsEmpty)
			{
				var summon = ObjectAccessor.GetCreature(unit, summonGuid);

				if (summon != null)
					if (_visited.Add(summon))
						func(summon);
			}

		var vehicle = unit.VehicleKit1;

		if (vehicle != null)
			foreach (var seatPair in vehicle.Seats)
			{
				var passenger = Global.ObjAccessor.GetUnit(unit, seatPair.Value.Passenger.Guid);

				if (passenger != null && passenger != unit)
					if (_visited.Add(passenger))
						func(passenger);
			}
	}
}