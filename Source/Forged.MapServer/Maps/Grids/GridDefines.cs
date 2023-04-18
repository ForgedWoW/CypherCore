// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Maps.Grids;

public class GridDefines
{
    private readonly MapManager _mapManager;
    public static GridDefines Instance { get; private set; }

    public GridDefines(MapManager mapManager)
    {
        _mapManager = mapManager;
        Instance = this;
    }

    public CellCoord ComputeCellCoord(float x, float y)
    {
        var xOffset = (x - MapConst.CenterGridCellOffset) / MapConst.SizeofCells;
        var yOffset = (y - MapConst.CenterGridCellOffset) / MapConst.SizeofCells;

        var xVal = (uint)(xOffset + MapConst.CenterGridCellId + 0.5f);
        var yVal = (uint)(yOffset + MapConst.CenterGridCellId + 0.5f);

        return new CellCoord(xVal, yVal);
    }

    public GridCoord ComputeGridCoord(float x, float y)
    {
        var xOffset = (x - MapConst.CenterGridOffset) / MapConst.SizeofGrids;
        var yOffset = (y - MapConst.CenterGridOffset) / MapConst.SizeofGrids;

        var xVal = (uint)(xOffset + MapConst.CenterGridId + 0.5f);
        var yVal = (uint)(yOffset + MapConst.CenterGridId + 0.5f);

        return new GridCoord(xVal, yVal);
    }

    public GridCoord ComputeGridCoordSimple(float x, float y)
    {
        var gx = (int)(MapConst.CenterGridId - x / MapConst.SizeofGrids);
        var gy = (int)(MapConst.CenterGridId - y / MapConst.SizeofGrids);

        return new GridCoord((uint)(MapConst.MaxGrids - 1 - gx), (uint)(MapConst.MaxGrids - 1 - gy));
    }

    public bool IsValidMapCoord(float c)
    {
        return float.IsFinite(c) && Math.Abs(c) <= MapConst.MapHalfSize - 0.5f;
    }

    public bool IsValidMapCoord(float x, float y)
    {
        return IsValidMapCoord(x) && IsValidMapCoord(y);
    }

    public bool IsValidMapCoord(Position pos)
    {
        return IsValidMapCoord(pos.X, pos.Y) && IsValidMapCoord(pos.Z);
    }

    public bool IsValidMapCoord(float x, float y, float z)
    {
        return IsValidMapCoord(x, y) && IsValidMapCoord(z);
    }

    public bool IsValidMapCoord(float x, float y, float z, float o)
    {
        return IsValidMapCoord(x, y, z) && float.IsFinite(o);
    }

    public bool IsValidMapCoord(uint mapid, float x, float y)
    {
        return _mapManager.IsValidMap(mapid) && IsValidMapCoord(x, y);
    }

    public bool IsValidMapCoord(uint mapid, float x, float y, float z)
    {
        return _mapManager.IsValidMap(mapid) && IsValidMapCoord(x, y, z);
    }

    public bool IsValidMapCoord(uint mapid, float x, float y, float z, float o)
    {
        return _mapManager.IsValidMap(mapid) && IsValidMapCoord(x, y, z, o);
    }

    public bool IsValidMapCoord(uint mapid, Position pos)
    {
        return IsValidMapCoord(mapid, pos.X, pos.Y, pos.Z, pos.Orientation);
    }

    public bool IsValidMapCoord(WorldLocation loc)
    {
        return IsValidMapCoord(loc.MapId, loc);
    }

    public float NormalizeMapCoord(float c)
    {
        c = c switch
        {
            > MapConst.MapHalfSize - 0.5f    => MapConst.MapHalfSize - 0.5f,
            < -(MapConst.MapHalfSize - 0.5f) => -(MapConst.MapHalfSize - 0.5f),
            _                                => c
        };

        return c;
    }
}