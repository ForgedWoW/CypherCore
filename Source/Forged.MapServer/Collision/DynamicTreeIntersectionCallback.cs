// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Collision.Models;
using Forged.MapServer.Phasing;
using Framework.Constants;
using Framework.GameMath;

namespace Forged.MapServer.Collision;

public class DynamicTreeIntersectionCallback : WorkerCallback
{
    private readonly PhaseShift _phaseShift;

    public DynamicTreeIntersectionCallback(PhaseShift phaseShift)
    {
        DidHit = false;
        _phaseShift = phaseShift;
    }

    public bool DidHit { get; private set; }

    public override bool Invoke(Ray r, Model obj, ref float distance)
    {
        DidHit = obj.IntersectRay(r, ref distance, true, _phaseShift, ModelIgnoreFlags.Nothing);

        return DidHit;
    }
}