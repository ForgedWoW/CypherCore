﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.Checks;

public class AnyUnfriendlyUnitInObjectRangeCheck : ICheck<Unit>
{
    private readonly Func<Unit, bool> _additionalCheck;
    private readonly Unit _funit;
    private readonly WorldObject _obj;
    private readonly float _range;

    public AnyUnfriendlyUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, Func<Unit, bool> additionalCheck = null)
    {
        _obj = obj;
        _funit = funit;
        _range = range;
        _additionalCheck = additionalCheck;
    }

    public bool Invoke(Unit u)
    {
        return u.IsAlive && _obj.Location.IsWithinDist(u, _range) && !_funit.WorldObjectCombat.IsFriendlyTo(u) && (_additionalCheck == null || _additionalCheck.Invoke(u));
    }
}