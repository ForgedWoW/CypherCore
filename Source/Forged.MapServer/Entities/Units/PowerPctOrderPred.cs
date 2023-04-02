// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

public class PowerPctOrderPred : IComparer<WorldObject>
{
    private readonly bool _ascending;
    private readonly PowerType _power;
    public PowerPctOrderPred(PowerType power, bool ascending = true)
    {
        _power = power;
        _ascending = ascending;
    }

    public int Compare(WorldObject objA, WorldObject objB)
    {
        var a = objA.AsUnit;
        var b = objB.AsUnit;
        var rA = a?.GetPowerPct(_power) ?? 0.0f;
        var rB = b?.GetPowerPct(_power) ?? 0.0f;

        return Convert.ToInt32(_ascending ? rA < rB : rA > rB);
    }
}