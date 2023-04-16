// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;

namespace Forged.MapServer.Phasing;

internal class ControlledUnitVisitor
{
    private readonly HashSet<WorldObject> _visited = new();

    public ControlledUnitVisitor(WorldObject owner)
    {
        _visited.Add(owner);
    }

    public void VisitControlledOf(Unit unit, Action<Unit> func)
    {
        foreach (var controlled in unit.Controlled.Where(controlled => !controlled.IsPlayer && controlled.Vehicle == null).Where(controlled => _visited.Add(controlled)))
            func(controlled);

        foreach (var summonGuid in unit.SummonSlot)
            if (!summonGuid.IsEmpty)
            {
                var summon = ObjectAccessor.GetCreature(unit, summonGuid);

                if (summon == null)
                    continue;

                if (_visited.Add(summon))
                    func(summon);
            }

        var vehicle = unit.VehicleKit;

        if (vehicle == null)
            return;

        foreach (var seatPair in vehicle.Seats)
        {
            var passenger = unit.ObjectAccessor.GetUnit(unit, seatPair.Value.Passenger.Guid);

            if (passenger == null || passenger == unit)
                continue;

            if (_visited.Add(passenger))
                func(passenger);
        }
    }
}