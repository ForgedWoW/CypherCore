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
    private readonly Cell _cell;
    private readonly uint _creatures;
    private readonly Garrison _garrison;
    private readonly Grid _grid;
    private readonly GarrisonMap _map;
    private uint _gameObjects;

    public GridType GridType { get; set; }

    public GarrisonGridLoader(Grid grid, GarrisonMap map, Cell cell, GridType gridType = GridType.Grid)
    {
        _cell = cell;
        _grid = grid;
        _map = map;
        _garrison = map.GetGarrison();
        GridType = gridType;
    }

    public void Visit(IList<GameObject> objs)
    {
        var plots = _garrison.GetPlots();

        if (plots.Empty())
            return;

        var cellCoord = _cell.CellCoord;

        foreach (var plot in plots)
        {
            var spawn = plot.PacketInfo.PlotPos;

            if (cellCoord != GridDefines.ComputeCellCoord(spawn.X, spawn.Y))
                continue;

            var go = plot.CreateGameObject(_map, _garrison.GetFaction());

            if (!go)
                continue;

            var cell = new Cell(cellCoord);
            _map.AddToGrid(go, cell);
            go.AddToWorld();
            ++_gameObjects;
        }
    }

    public void LoadN()
    {
        if (_garrison != null)
        {
            _cell.Data.CellY = 0;

            for (uint x = 0; x < MapConst.MaxCells; ++x)
            {
                _cell.Data.CellX = x;

                for (uint y = 0; y < MapConst.MaxCells; ++y)
                {
                    _cell.Data.CellY = y;

                    //Load creatures and GameInfo objects
                    _grid.VisitGrid(x, y, this);
                }
            }
        }

        Log.Logger.Debug("{0} GameObjects and {1} Creatures loaded for grid {2} on map {3}", _gameObjects, _creatures, _grid.GridId, _map.Id);
    }
}