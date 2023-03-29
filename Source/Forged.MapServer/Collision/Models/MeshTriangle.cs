// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Collision.Models;

public struct MeshTriangle
{
    public readonly int Idx0;
    public readonly int Idx1;
    public readonly int Idx2;

    public MeshTriangle(int na, int nb, int nc)
    {
        Idx0 = na;
        Idx1 = nb;
        Idx2 = nc;
    }
}