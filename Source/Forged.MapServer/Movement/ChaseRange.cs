// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Movement;

public struct ChaseRange
{
    public ChaseRange(float range)
    {
        MinRange = range > SharedConst.ContactDistance ? 0 : range - SharedConst.ContactDistance;
        MinTolerance = range;
        MaxRange = range + SharedConst.ContactDistance;
        MaxTolerance = range;
    }

    // ...and if we are, we will move into this range
    public ChaseRange(float min, float max)
    {
        MinRange = min;
        MinTolerance = Math.Min(min + SharedConst.ContactDistance, (min + max) / 2);
        MaxRange = max;
        MaxTolerance = Math.Max(max - SharedConst.ContactDistance, MinTolerance);
    }

    public ChaseRange(float min, float tMin, float tMax, float max)
    {
        MinRange = min;
        MinTolerance = tMin;
        MaxRange = max;
        MaxTolerance = tMax;
    }

    public float MaxRange { get; set; }

    // we have to move if we are outside this range...   (max. attack range)
    public float MaxTolerance { get; set; }

    // this contains info that informs how we should path!
    public float MinRange { get; set; } // we have to move if we are within this range...    (min. attack range)

    public float MinTolerance { get; set; } // ...and if we are, we will move this far away
}