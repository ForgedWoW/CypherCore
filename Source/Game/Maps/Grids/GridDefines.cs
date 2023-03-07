// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Grids;

public class GridDefines
{
	public static bool IsValidMapCoord(float c)
	{
		return float.IsFinite(c) && Math.Abs(c) <= MapConst.MapHalfSize - 0.5f;
	}

	public static bool IsValidMapCoord(float x, float y)
	{
		return IsValidMapCoord(x) && IsValidMapCoord(y);
	}

    public static bool IsValidMapCoord(Position pos)
    {
        return IsValidMapCoord(pos.X, pos.Y) && IsValidMapCoord(pos.Z);
    }

    public static bool IsValidMapCoord(float x, float y, float z)
	{
		return IsValidMapCoord(x, y) && IsValidMapCoord(z);
	}

	public static bool IsValidMapCoord(float x, float y, float z, float o)
	{
		return IsValidMapCoord(x, y, z) && float.IsFinite(o);
	}

	public static bool IsValidMapCoord(uint mapid, float x, float y)
	{
		return Global.MapMgr.IsValidMAP(mapid) && IsValidMapCoord(x, y);
	}

	public static bool IsValidMapCoord(uint mapid, float x, float y, float z)
	{
		return Global.MapMgr.IsValidMAP(mapid) && IsValidMapCoord(x, y, z);
	}

	public static bool IsValidMapCoord(uint mapid, float x, float y, float z, float o)
	{
		return Global.MapMgr.IsValidMAP(mapid) && IsValidMapCoord(x, y, z, o);
	}

	public static bool IsValidMapCoord(uint mapid, Position pos)
	{
		return IsValidMapCoord(mapid, pos.X, pos.Y, pos.Z, pos.Orientation);
	}

	public static bool IsValidMapCoord(WorldLocation loc)
	{
		return IsValidMapCoord(loc.GetMapId(), loc);
	}

	public static float NormalizeMapCoord(float c)
	{
		if (c > MapConst.MapHalfSize - 0.5f)
			c = MapConst.MapHalfSize - 0.5f;
		else if (c < -(MapConst.MapHalfSize - 0.5f))
			c = -(MapConst.MapHalfSize - 0.5f);

		return c;
	}

	public static GridCoord ComputeGridCoord(float x, float y)
	{
		var x_offset = (x - MapConst.CenterGridOffset) / MapConst.SizeofGrids;
		var y_offset = (y - MapConst.CenterGridOffset) / MapConst.SizeofGrids;

		var x_val = (uint)(x_offset + MapConst.CenterGridId + 0.5f);
		var y_val = (uint)(y_offset + MapConst.CenterGridId + 0.5f);

		return new GridCoord(x_val, y_val);
	}

	public static GridCoord ComputeGridCoordSimple(float x, float y)
	{
		var gx = (int)(MapConst.CenterGridId - x / MapConst.SizeofGrids);
		var gy = (int)(MapConst.CenterGridId - y / MapConst.SizeofGrids);

		return new GridCoord((uint)(MapConst.MaxGrids - 1 - gx), (uint)(MapConst.MaxGrids - 1 - gy));
	}

	public static CellCoord ComputeCellCoord(float x, float y)
	{
		var x_offset = (x - MapConst.CenterGridCellOffset) / MapConst.SizeofCells;
		var y_offset = (y - MapConst.CenterGridCellOffset) / MapConst.SizeofCells;

		var x_val = (uint)(x_offset + MapConst.CenterGridCellId + 0.5f);
		var y_val = (uint)(y_offset + MapConst.CenterGridCellId + 0.5f);

		return new CellCoord(x_val, y_val);
	}
}