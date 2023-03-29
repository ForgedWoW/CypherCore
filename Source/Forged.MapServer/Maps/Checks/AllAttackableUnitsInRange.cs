// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.Checks;

internal class AllAttackableUnitsInRange : ICheck<Unit>
{
    private readonly Unit _unit;
    private readonly float _range;

    public AllAttackableUnitsInRange(Unit obj, float range)
    {
        _unit = obj;
        _range = range;
    }

    public bool Invoke(Unit u)
    {
        if (!u.IsAlive)
            return false;

        if (!u.IsVisible())
            return false;

        if (!_unit.WorldObjectCombat.IsValidAttackTarget(u))
            return false;

        if (_range != 0f)
        {
            if (_range > 0.0f && !_unit.Location.IsWithinDist(u, _range, false))
                return false;

            if (_range < 0.0f && _unit.Location.IsWithinDist(u, _range, false))
                return false;
        }

        return true;
    }
}