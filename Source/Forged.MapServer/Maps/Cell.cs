// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps;

public class Cell
{
    public CellMetadata Data;

    public Cell(ICoord p, GridDefines gridDefines)
    {
        CellCalculator = new CellCalculator(gridDefines);
        GridDefines = gridDefines;
        Data.GridX = p.X / MapConst.MaxCells;
        Data.GridY = p.Y / MapConst.MaxCells;
        Data.CellX = p.X % MapConst.MaxCells;
        Data.CellY = p.Y % MapConst.MaxCells;
    }

    public Cell(float x, float y, GridDefines gridDefines)
    {
        CellCalculator = new CellCalculator(gridDefines);
        ICoord p = gridDefines.ComputeCellCoord(x, y);
        Data.GridX = p.X / MapConst.MaxCells;
        Data.GridY = p.Y / MapConst.MaxCells;
        Data.CellX = p.X % MapConst.MaxCells;
        Data.CellY = p.Y % MapConst.MaxCells;
        GridDefines = gridDefines;
    }

    public Cell(Cell cell, GridDefines gridDefines)
    {
        CellCalculator = new CellCalculator(gridDefines);
        Data = cell.Data;
    }

    public CellCalculator CellCalculator { get; }

    public CellCoord CellCoord =>
        new(Data.GridX * MapConst.MaxCells + Data.CellX,
            Data.GridY * MapConst.MaxCells + Data.CellY);

    public GridDefines GridDefines { get; }
    public uint Id => Data.GridX * MapConst.MaxGrids + Data.GridY;

    public bool IsCellValid => Data is { CellX: < MapConst.MaxCells, CellY: < MapConst.MaxCells };

    public static bool operator !=(Cell left, Cell right)
    {
        return !(left == right);
    }

    public static bool operator ==(Cell left, Cell right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            return false;

        return left.Data.CellX == right.Data.CellX && left.Data.CellY == right.Data.CellY && left.Data.GridX == right.Data.GridX && left.Data.GridY == right.Data.GridY;
    }

    public bool DiffCell(Cell cell)
    {
        return Data.CellX != cell.Data.CellX ||
               Data.CellY != cell.Data.CellY;
    }

    public bool DiffGrid(Cell cell)
    {
        return Data.GridX != cell.Data.GridX ||
               Data.GridY != cell.Data.GridY;
    }

    public override bool Equals(object obj)
    {
        return obj is Cell cell && this == cell;
    }

    public override int GetHashCode()
    {
        return (int)(Data.CellX ^ Data.CellY ^ Data.GridX ^ Data.GridY);
    }

    public override string ToString()
    {
        return $"grid[{Data.GridX}, {Data.GridY}]cell[{Data.CellX}, {Data.CellY}]";
    }

    public void Visit(CellCoord standingCell, IGridNotifier visitor, Map map, WorldObject obj, float radius)
    {
        //we should increase search radius by object's radius, otherwise
        //we could have problems with huge creatures, which won't attack nearest players etc
        Visit(standingCell, visitor, map, obj.Location.X, obj.Location.Y, radius + obj.CombatReach);
    }

    public void Visit(CellCoord standingCell, IGridNotifier visitor, Map map, float xOff, float yOff, float radius)
    {
        if (!standingCell.IsCoordValid())
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
        var area = CellCalculator.CalculateCellArea(xOff, yOff, radius);

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
        if (area.HighBound.X > area.LowBound.X + 4 && area.HighBound.Y > area.LowBound.Y + 4)
        {
            VisitCircle(visitor, map, area.LowBound, area.HighBound);

            return;
        }

        //ALWAYS visit standing cell first!!! Since we deal with small radiuses
        //it is very essential to call visitor for standing cell firstly...
        map.Visit(this, visitor);

        // loop the cell range
        for (var x = area.LowBound.X; x <= area.HighBound.X; ++x)
        {
            for (var y = area.LowBound.Y; y <= area.HighBound.Y; ++y)
            {
                CellCoord cellCoord = new(x, y);

                //lets skip standing cell since we already visited it
                if (cellCoord == standingCell)
                    continue;

                Cell rZone = new(cellCoord, GridDefines);
                rZone.Data.NoCreate = Data.NoCreate;
                map.Visit(rZone, visitor);
            }
        }
    }

    private void VisitCircle(IGridNotifier visitor, Map map, ICoord beginCell, ICoord endCell)
    {
        //here is an algorithm for 'filling' circum-squared octagon
        var xShift = (uint)Math.Ceiling((endCell.X - beginCell.X) * 0.3f - 0.5f);
        //lets calculate x_start/x_end coords for central strip...
        var xStart = beginCell.X + xShift;
        var xEnd = endCell.X - xShift;

        //visit central strip with constant width...
        for (var x = xStart; x <= xEnd; ++x)
        {
            for (var y = beginCell.Y; y <= endCell.Y; ++y)
            {
                CellCoord cellCoord = new(x, y);
                Cell rZone = new(cellCoord, GridDefines);
                rZone.Data.NoCreate = Data.NoCreate;
                map.Visit(rZone, visitor);
            }
        }

        //if x_shift == 0 then we have too small cell area, which were already
        //visited at previous step, so just return from procedure...
        if (xShift == 0)
            return;

        var yStart = endCell.Y;
        var yEnd = beginCell.Y;

        //now we are visiting borders of an octagon...
        for (uint step = 1; step <= xStart - beginCell.X; ++step)
        {
            //each step reduces strip height by 2 cells...
            yEnd += 1;
            yStart -= 1;

            for (var y = yStart; y >= yEnd; --y)
            {
                //we visit cells symmetrically from both sides, heading from center to sides and from up to bottom
                //e.g. filling 2 trapezoids after filling central cell strip...
                CellCoord cellCoordLeft = new(xStart - step, y);
                Cell rZoneLeft = new(cellCoordLeft, GridDefines);
                rZoneLeft.Data.NoCreate = Data.NoCreate;
                map.Visit(rZoneLeft, visitor);

                //right trapezoid cell visit
                CellCoord cellCoordRight = new(xEnd + step, y);
                Cell rZoneRight = new(cellCoordRight, GridDefines);
                rZoneRight.Data.NoCreate = Data.NoCreate;
                map.Visit(rZoneRight, visitor);
            }
        }
    }

    public struct CellMetadata
    {
        public uint CellX;
        public uint CellY;
        public uint GridX;
        public uint GridY;
        public bool NoCreate;
    }
}