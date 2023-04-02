// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;

namespace Forged.MapServer.Movement;

public class SplineChainLink
{
    public uint ExpectedDuration { get; set; }
    public List<Vector3> Points { get; set; } = new();
    public uint TimeToNext { get; set; }
    public float Velocity { get; set; }

    public SplineChainLink(Vector3[] points, uint expectedDuration, uint msToNext, float velocity)
    {
        Points.AddRange(points);
        ExpectedDuration = expectedDuration;
        TimeToNext = msToNext;
        Velocity = velocity;
    }

    public SplineChainLink(uint expectedDuration, uint msToNext, float velocity)
    {
        ExpectedDuration = expectedDuration;
        TimeToNext = msToNext;
        Velocity = velocity;
    }
}