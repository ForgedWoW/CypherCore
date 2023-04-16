// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Movement.Generators;

namespace Forged.MapServer.Movement;

internal class MovementGeneratorComparator : IComparer<MovementGenerator>
{
    public int Compare(MovementGenerator a, MovementGenerator b)
    {
        if (a == null || b == null)
            return 0;

        if (a.Equals(b))
            return 0;

        if (a.Mode < b.Mode)
        {
            return 1;
        }

        if (a.Mode == b.Mode)
        {
            if ((int)a.Priority < (int)b.Priority)
                return 1;

            if (a.Priority == b.Priority)
                return 0;
        }

        return -1;
    }
}