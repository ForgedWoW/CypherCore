// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.GridNotifiers;

internal class MostHpPercentMissingInRange : ICheck<Unit>
{
    private readonly float _maxHpPct;
    private readonly float _minHpPct;
    private readonly Unit _obj;
    private readonly float _range;
    private float _hpPct;

    public MostHpPercentMissingInRange(Unit obj, float range, uint minHpPct, uint maxHpPct)
    {
        _obj = obj;
        _range = range;
        _minHpPct = minHpPct;
        _maxHpPct = maxHpPct;
        _hpPct = 101.0f;
    }

    public bool Invoke(Unit u)
    {
        if (!u.IsAlive || !u.IsInCombat || _obj.WorldObjectCombat.IsHostileTo(u) || !_obj.Location.IsWithinDist(u, _range) || !(_minHpPct <= u.HealthPct) || !(u.HealthPct <= _maxHpPct) || !(u.HealthPct < _hpPct))
            return false;

        _hpPct = u.HealthPct;

        return true;
    }
}