﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
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
        X_Coord = x;
        Y_Coord = y;
    }

    public GridCoord(GridCoord obj)
    {
        X_Coord = obj.X_Coord;
        Y_Coord = obj.Y_Coord;
    }

    public uint X_Coord { get; set; }
    public uint Y_Coord { get; set; }
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

    public void Dec_x(uint val)
    {
        if (X_Coord > val)
            X_Coord -= val;
        else
            X_Coord = 0;
    }

    public void Dec_y(uint val)
    {
        if (Y_Coord > val)
            Y_Coord -= val;
        else
            Y_Coord = 0;
    }

    public override bool Equals(object obj)
    {
        return obj is ObjectGuid && Equals((ObjectGuid)obj);
    }

    public bool Equals(GridCoord other)
    {
        return other.X_Coord == X_Coord && other.Y_Coord == Y_Coord;
    }

    public override int GetHashCode()
    {
        return new
        {
            X_Coord,
            Y_Coord
        }.GetHashCode();
    }

    public uint GetId()
    {
        return Y_Coord * Limit + X_Coord;
    }

    public void Inc_x(uint val)
    {
        if (X_Coord + val < Limit)
            X_Coord += val;
        else
            X_Coord = Limit - 1;
    }

    public void Inc_y(uint val)
    {
        if (Y_Coord + val < Limit)
            Y_Coord += val;
        else
            Y_Coord = Limit - 1;
    }

    public bool IsCoordValid()
    {
        return X_Coord < Limit && Y_Coord < Limit;
    }

    public ICoord Normalize()
    {
        X_Coord = Math.Min(X_Coord, Limit - 1);
        Y_Coord = Math.Min(Y_Coord, Limit - 1);

        return this;
    }
}