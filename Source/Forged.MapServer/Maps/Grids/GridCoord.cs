// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Maps.Grids;

public class GridCoord : ICoord
{
    private const int Limit = MapConst.MaxGrids;


    public GridCoord(uint x, uint y)
    {
        X = x;
        Y = y;
    }

    public GridCoord(GridCoord obj)
    {
        X = obj.X;
        Y = obj.Y;
    }

    public uint X { get; set; }
    public uint Y { get; set; }
    public static bool operator !=(GridCoord first, GridCoord other)
    {
        return !(first == other);
    }

    public static bool operator ==(GridCoord first, GridCoord other)
    {
        if (ReferenceEquals(first, other))
            return true;

        if (ReferenceEquals(first, null) || ReferenceEquals(other, null))
            return false;

        return first.Equals(other);
    }

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

    public override bool Equals(object obj)
    {
        return obj is ObjectGuid guid && Equals(guid);
    }

    public bool Equals(GridCoord other)
    {
        return other.X == X && other.Y == Y;
    }

    public override int GetHashCode()
    {
        return new
        {
            X_Coord = X,
            Y_Coord = Y
        }.GetHashCode();
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
}