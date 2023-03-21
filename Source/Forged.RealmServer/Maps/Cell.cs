// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Maps.Grids;
using Forged.RealmServer.Maps.Interfaces;

namespace Forged.RealmServer.Maps;

public class Cell
{
	public CellMetadata Data;

	public Cell(ICoord p)
	{
		Data.Gridx = p.X_Coord / MapConst.MaxCells;
		Data.Gridy = p.Y_Coord / MapConst.MaxCells;
		Data.Cellx = p.X_Coord % MapConst.MaxCells;
		Data.Celly = p.Y_Coord % MapConst.MaxCells;
	}

	public Cell(float x, float y)
	{
		ICoord p = GridDefines.ComputeCellCoord(x, y);
		Data.Gridx = p.X_Coord / MapConst.MaxCells;
		Data.Gridy = p.Y_Coord / MapConst.MaxCells;
		Data.Cellx = p.X_Coord % MapConst.MaxCells;
		Data.Celly = p.Y_Coord % MapConst.MaxCells;
	}

	public Cell(Cell cell)
	{
		Data = cell.Data;
	}

	public bool IsCellValid()
	{
		return Data.Cellx < MapConst.MaxCells && Data.Celly < MapConst.MaxCells;
	}

	public uint GetId()
	{
		return Data.Gridx * MapConst.MaxGrids + Data.Gridy;
	}

	public uint GetCellX()
	{
		return Data.Cellx;
	}

	public uint GetCellY()
	{
		return Data.Celly;
	}

	public uint GetGridX()
	{
		return Data.Gridx;
	}

	public uint GetGridY()
	{
		return Data.Gridy;
	}

	public bool NoCreate()
	{
		return Data.NoCreate;
	}

	public void SetNoCreate()
	{
		Data.NoCreate = true;
	}

	public static bool operator ==(Cell left, Cell right)
	{
		if (ReferenceEquals(left, right))
			return true;

		if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
			return false;

		return left.Data.Cellx == right.Data.Cellx && left.Data.Celly == right.Data.Celly && left.Data.Gridx == right.Data.Gridx && left.Data.Gridy == right.Data.Gridy;
	}

	public static bool operator !=(Cell left, Cell right)
	{
		return !(left == right);
	}

	public override bool Equals(object obj)
	{
		return obj is Cell && this == (Cell)obj;
	}

	public override int GetHashCode()
	{
		return (int)(Data.Cellx ^ Data.Celly ^ Data.Gridx ^ Data.Gridy);
	}

	public override string ToString()
	{
		return $"grid[{GetGridX()}, {GetGridY()}]cell[{GetCellX()}, {GetCellY()}]";
	}

	public CellCoord GetCellCoord()
	{
		return new CellCoord(Data.Gridx * MapConst.MaxCells + Data.Cellx,
							Data.Gridy * MapConst.MaxCells + Data.Celly);
	}

	public bool DiffCell(Cell cell)
	{
		return (Data.Cellx != cell.Data.Cellx ||
				Data.Celly != cell.Data.Celly);
	}

	public bool DiffGrid(Cell cell)
	{
		return (Data.Gridx != cell.Data.Gridx ||
				Data.Gridy != cell.Data.Gridy);
	}

	public void Visit(CellCoord standing_cell, IGridNotifier visitor, Map map, WorldObject obj, float radius)
	{
		//we should increase search radius by object's radius, otherwise
		//we could have problems with huge creatures, which won't attack nearest players etc
		Visit(standing_cell, visitor, map, obj.Location.X, obj.Location.Y, radius + obj.CombatReach);
	}

	public void Visit(CellCoord standing_cell, IGridNotifier visitor, Map map, float x_off, float y_off, float radius)
	{
		if (!standing_cell.IsCoordValid())
			return;

		//no jokes here... Actually placing ASSERT() here was good idea, but
		//we had some problems with DynamicObjects, which pass radius = 0.0f (DB issue?)
		//maybe it is better to just return when radius <= 0.0f?
		if (radius <= 0.0f)
		{
			map.Visit(this, visitor);

			return;
		}

		//lets limit the upper value for search radius
		if (radius > MapConst.SizeofGrids)
			radius = MapConst.SizeofGrids;

		//lets calculate object coord offsets from cell borders.
		var area = CalculateCellArea(x_off, y_off, radius);

		//if radius fits inside standing cell
		if (area == null)
		{
			map.Visit(this, visitor);

			return;
		}

		//visit all cells, found in CalculateCellArea()
		//if radius is known to reach cell area more than 4x4 then we should call optimized VisitCircle
		//currently this technique works with MAX_NUMBER_OF_CELLS 16 and higher, with lower values
		//there are nothing to optimize because SIZE_OF_GRID_CELL is too big...
		if ((area.HighBound.X_Coord > (area.LowBound.X_Coord + 4)) && (area.HighBound.Y_Coord > (area.LowBound.Y_Coord + 4)))
		{
			VisitCircle(visitor, map, area.LowBound, area.HighBound);

			return;
		}

		//ALWAYS visit standing cell first!!! Since we deal with small radiuses
		//it is very essential to call visitor for standing cell firstly...
		map.Visit(this, visitor);

		// loop the cell range
		for (var x = area.LowBound.X_Coord; x <= area.HighBound.X_Coord; ++x)
		{
			for (var y = area.LowBound.Y_Coord; y <= area.HighBound.Y_Coord; ++y)
			{
				CellCoord cellCoord = new(x, y);

				//lets skip standing cell since we already visited it
				if (cellCoord != standing_cell)
				{
					Cell r_zone = new(cellCoord);
					r_zone.Data.NoCreate = Data.NoCreate;
					map.Visit(r_zone, visitor);
				}
			}
		}
	}

	public static void VisitGrid(WorldObject center_obj, IGridNotifier visitor, float radius, bool dont_load = true)
	{
		var p = GridDefines.ComputeCellCoord(center_obj.Location.X, center_obj.Location.Y);
		Cell cell = new(p);

		if (dont_load)
			cell.SetNoCreate();

		cell.Visit(p, visitor, center_obj.Map, center_obj, radius);
	}

	public static void VisitGrid(float x, float y, Map map, IGridNotifier visitor, float radius, bool dont_load = true)
	{
		var p = GridDefines.ComputeCellCoord(x, y);
		Cell cell = new(p);

		if (dont_load)
			cell.SetNoCreate();

		cell.Visit(p, visitor, map, x, y, radius);
	}

	public static CellArea CalculateCellArea(float x, float y, float radius)
	{
		if (radius <= 0.0f)
		{
			var center = (CellCoord)GridDefines.ComputeCellCoord(x, y).Normalize();

			return new CellArea(center, center);
		}

		var centerX = (CellCoord)GridDefines.ComputeCellCoord(x - radius, y - radius).Normalize();
		var centerY = (CellCoord)GridDefines.ComputeCellCoord(x + radius, y + radius).Normalize();

		return new CellArea(centerX, centerY);
	}

	void VisitCircle(IGridNotifier visitor, Map map, ICoord begin_cell, ICoord end_cell)
	{
		//here is an algorithm for 'filling' circum-squared octagon
		var x_shift = (uint)Math.Ceiling((end_cell.X_Coord - begin_cell.X_Coord) * 0.3f - 0.5f);
		//lets calculate x_start/x_end coords for central strip...
		var x_start = begin_cell.X_Coord + x_shift;
		var x_end = end_cell.X_Coord - x_shift;

		//visit central strip with constant width...
		for (var x = x_start; x <= x_end; ++x)
		{
			for (var y = begin_cell.Y_Coord; y <= end_cell.Y_Coord; ++y)
			{
				CellCoord cellCoord = new(x, y);
				Cell r_zone = new(cellCoord);
				r_zone.Data.NoCreate = Data.NoCreate;
				map.Visit(r_zone, visitor);
			}
		}

		//if x_shift == 0 then we have too small cell area, which were already
		//visited at previous step, so just return from procedure...
		if (x_shift == 0)
			return;

		var y_start = end_cell.Y_Coord;
		var y_end = begin_cell.Y_Coord;

		//now we are visiting borders of an octagon...
		for (uint step = 1; step <= (x_start - begin_cell.X_Coord); ++step)
		{
			//each step reduces strip height by 2 cells...
			y_end += 1;
			y_start -= 1;

			for (var y = y_start; y >= y_end; --y)
			{
				//we visit cells symmetrically from both sides, heading from center to sides and from up to bottom
				//e.g. filling 2 trapezoids after filling central cell strip...
				CellCoord cellCoord_left = new(x_start - step, y);
				Cell r_zone_left = new(cellCoord_left);
				r_zone_left.Data.NoCreate = Data.NoCreate;
				map.Visit(r_zone_left, visitor);

				//right trapezoid cell visit
				CellCoord cellCoord_right = new(x_end + step, y);
				Cell r_zone_right = new(cellCoord_right);
				r_zone_right.Data.NoCreate = Data.NoCreate;
				map.Visit(r_zone_right, visitor);
			}
		}
	}

	public struct CellMetadata
	{
		public uint Gridx;
		public uint Gridy;
		public uint Cellx;
		public uint Celly;
		public bool NoCreate;
	}
}