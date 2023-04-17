﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.GridNotifiers;

public class MostHpMissingInRange<T> : ICheck<T> where T : Unit
{
    private readonly Unit _obj;
    private readonly float _range;
    private long _hp;

    public MostHpMissingInRange(Unit obj, float range, uint hp)
    {
        _obj = obj;
        _range = range;
        _hp = hp;
    }

    public bool Invoke(T u)
    {
        if (!u.IsAlive || !u.IsInCombat || _obj.WorldObjectCombat.IsHostileTo(u) || !_obj.Location.IsWithinDist(u, _range) || u.MaxHealth - u.Health <= _hp)
            return false;

        _hp = (u.MaxHealth - u.Health);

        return true;
    }
}