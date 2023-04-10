// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Collision.Maps;
using Forged.MapServer.Collision.Models;
using Forged.MapServer.Phasing;

namespace Forged.MapServer.Collision;

public class DynamicTreeAreaInfoCallback : WorkerCallback
{
    private readonly PhaseShift _phaseShift;

    public AreaInfo AreaInfo { get; }

    public DynamicTreeAreaInfoCallback(PhaseShift phaseShift)
    {
        _phaseShift = phaseShift;
        AreaInfo = new AreaInfo();
    }

    public override void Invoke(Vector3 p, GameObjectModel obj)
    {
        obj.IntersectPoint(p, AreaInfo, _phaseShift);
    }
}