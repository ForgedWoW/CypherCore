// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Garrisons;

internal class GarrisonGridLoader : IGridNotifierGameObject
{
    private readonly Cell i_cell;
    private readonly uint i_creatures;
    private readonly Garrison i_garrison;
    private readonly Grid i_grid;
    private readonly GarrisonMap i_map;
    private uint i_gameObjects;

    public GridType GridType { get; set; }

    public GarrisonGridLoader(Grid grid, GarrisonMap map, Cell cell, GridType gridType = GridType.Grid)
    {
        i_cell = cell;
        i_grid = grid;
        i_map = map;
        i_garrison = map.GetGarrison();
        GridType = gridType;
    }

    public void Visit(IList<GameObject> objs)
    {
        var plots = i_garrison.GetPlots();

        if (!plots.Empty())
        {
            var cellCoord = i_cell.GetCellCoord();

            foreach (var plot in plots)
            {
                var spawn = plot.PacketInfo.PlotPos;

                if (cellCoord != GridDefines.ComputeCellCoord(spawn.X, spawn.Y))
                    continue;

                var go = plot.CreateGameObject(i_map, i_garrison.GetFaction());

                if (!go)
                    continue;

                var cell = new Cell(cellCoord);
                i_map.AddToGrid(go, cell);
                go.AddToWorld();
                ++i_gameObjects;
            }
        }
    }

    public void LoadN()
    {
        if (i_garrison != null)
        {
            i_cell.Data.Celly = 0;

            for (uint x = 0; x < MapConst.MaxCells; ++x)
            {
                i_cell.Data.Cellx = x;

                for (uint y = 0; y < MapConst.MaxCells; ++y)
                {
                    i_cell.Data.Celly = y;

                    //Load creatures and GameInfo objects
                    i_grid.VisitGrid(x, y, this);
                }
            }
        }

        Log.Logger.Debug("{0} GameObjects and {1} Creatures loaded for grid {2} on map {3}", i_gameObjects, i_creatures, i_grid.GetGridId(), i_map.Id);
    }
}