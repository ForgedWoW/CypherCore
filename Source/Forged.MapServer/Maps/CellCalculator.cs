// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Interfaces;

namespace Forged.MapServer.Maps;

public class CellCalculator
{
    private readonly GridDefines _gridDefines;

    public CellCalculator(GridDefines gridDefines)
    {
        _gridDefines = gridDefines;
    }

    public CellArea CalculateCellArea(float x, float y, float radius)
    {
        if (radius <= 0.0f)
        {
            var center = (CellCoord)_gridDefines.ComputeCellCoord(x, y).Normalize();

            return new CellArea(center, center);
        }

        var centerX = (CellCoord)_gridDefines.ComputeCellCoord(x - radius, y - radius).Normalize();
        var centerY = (CellCoord)_gridDefines.ComputeCellCoord(x + radius, y + radius).Normalize();

        return new CellArea(centerX, centerY);
    }

    public void VisitGrid(WorldObject centerObj, IGridNotifier visitor, float radius, bool dontLoad = true)
    {
        var p = _gridDefines.ComputeCellCoord(centerObj.Location.X, centerObj.Location.Y);
        Cell cell = new(p, _gridDefines);

        if (dontLoad)
            cell.Data.NoCreate = true;

        ;

        cell.Visit(p, visitor, centerObj.Location.Map, centerObj, radius);
    }

    public void VisitGrid(float x, float y, Map map, IGridNotifier visitor, float radius, bool dontLoad = true)
    {
        var p = _gridDefines.ComputeCellCoord(x, y);
        Cell cell = new(p, _gridDefines);

        if (dontLoad)
            cell.Data.NoCreate = true;

        ;

        cell.Visit(p, visitor, map, x, y, radius);
    }
}