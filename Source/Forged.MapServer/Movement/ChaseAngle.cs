// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Movement;

public struct ChaseAngle
{
    public ChaseAngle(float angle, float tol = MathFunctions.PI_OVER4)
    {
        RelativeAngle = Position.NormalizeOrientation(angle);
        Tolerance = tol;
    }

    public float RelativeAngle { get; set; } // we want to be at this angle relative to the target (0 = front, M_PI = back)
    public float Tolerance { get; set; }     // but we'll tolerate anything within +- this much

    public bool IsAngleOkay(float relAngle)
    {
        var diff = Math.Abs(relAngle - RelativeAngle);

        return (Math.Min(diff, (2 * MathF.PI) - diff) <= Tolerance);
    }

    public float LowerBound()
    {
        return Position.NormalizeOrientation(RelativeAngle - Tolerance);
    }

    public float UpperBound()
    {
        return Position.NormalizeOrientation(RelativeAngle + Tolerance);
    }
}