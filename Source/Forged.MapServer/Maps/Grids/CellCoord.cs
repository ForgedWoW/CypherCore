// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Maps.Grids;

public class CellCoord : ICoord
{
    private const int Limit = MapConst.TotalCellsPerMap;

    public CellCoord(uint x, uint y)
    {
        X = x;
        Y = y;
    }

    public CellCoord(CellCoord obj)
    {
        X = obj.X;
        Y = obj.Y;
    }

    public uint X { get; set; }
    public uint Y { get; set; }

    public void DecX(uint val)
    {
        if (X > val)
            X -= val;
        else
            X = 0;
    }

    public void DecY(uint val)
    {
        if (Y > val)
            Y -= val;
        else
            Y = 0;
    }

    public uint GetId()
    {
        return Y * Limit + X;
    }

    public void IncX(uint val)
    {
        if (X + val < Limit)
            X += val;
        else
            X = Limit - 1;
    }

    public void IncY(uint val)
    {
        if (Y + val < Limit)
            Y += val;
        else
            Y = Limit - 1;
    }

    public bool IsCoordValid()
    {
        return X < Limit && Y < Limit;
    }

    public ICoord Normalize()
    {
        X = Math.Min(X, Limit - 1);
        Y = Math.Min(Y, Limit - 1);

        return this;
    }

    public static bool operator !=(CellCoord p1, CellCoord p2)
    {
        return !(p1 == p2);
    }

    public static bool operator ==(CellCoord p1, CellCoord p2)
    {
        return p2 != null && p1 != null && p1.X == p2.X && p1.Y == p2.Y;
    }

    public override bool Equals(object obj)
    {
        if (obj is CellCoord coord)
            return coord == this;

        return false;
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode();
    }
}