// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Maps.Grids;

public class Grid
{
    private readonly GridCell[][] _cells = new GridCell[MapConst.MaxCells][];

    public Grid(uint id, uint x, uint y, long expiry, bool unload = true)
    {
        GridId = id;
        X = x;
        Y = y;
        GridInformation = new GridInfo(expiry, unload);
        GridState = GridState.Invalid;
        IsGridObjectDataLoaded = false;

        for (uint xx = 0; xx < MapConst.MaxCells; ++xx)
        {
            _cells[xx] = new GridCell[MapConst.MaxCells];

            for (uint yy = 0; yy < MapConst.MaxCells; ++yy)
                _cells[xx][yy] = new GridCell();
        }
    }

    public Grid(Cell cell, uint expiry, bool unload = true) : this(cell.Id, cell.Data.GridX, cell.Data.GridY, expiry, unload) { }

    public uint GridId { get; }

    public GridInfo GridInformation { get; }

    public GridState GridState { get; set; }

    public bool IsGridObjectDataLoaded { get; set; }

    public uint X { get; }

    public uint Y { get; }

    public GridCell GetGridCell(uint x, uint y)
    {
        return _cells[x][y];
    }

    public uint GetWorldObjectCountInNGrid<T>() where T : WorldObject
    {
        uint count = 0;

        for (uint x = 0; x < MapConst.MaxCells; ++x)
            for (uint y = 0; y < MapConst.MaxCells; ++y)
                count += _cells[x][y].GetWorldObjectCountInGrid<T>();

        return count;
    }

    public void Update(Map map, uint diff)
    {
        switch (GridState)
        {
            case GridState.Active:
                // Only check grid activity every (grid_expiry/10) ms, because it's really useless to do it every cycle
                GridInformation.TimeTracker.Update(diff);

                if (GridInformation.TimeTracker.Passed)
                {
                    if (GetWorldObjectCountInNGrid<Player>() == 0 && !map.ActiveObjectsNearGrid(this))
                    {
                        ObjectGridStoper worker = new(GridType.Grid);
                        VisitAllGrids(worker);
                        GridState = GridState.Idle;

                        Log.Logger.Debug("Grid[{0}, {1}] on map {2} moved to IDLE state",
                                         X,
                                         Y,
                                         map.Id);
                    }
                    else
                        map.ResetGridExpiry(this, 0.1f);
                }

                break;

            case GridState.Idle:
                map.ResetGridExpiry(this);
                GridState = GridState.Removal;

                Log.Logger.Debug("Grid[{0}, {1}] on map {2} moved to REMOVAL state",
                                 X,
                                 Y,
                                 map.Id);

                break;

            case GridState.Removal:
                if (!GridInformation.UnloadLock)
                {
                    GridInformation.TimeTracker.Update(diff);

                    if (GridInformation.TimeTracker.Passed)
                        if (!map.UnloadGrid(this, false))
                        {
                            Log.Logger.Debug("Grid[{0}, {1}] for map {2} differed unloading due to players or active objects nearby",
                                             X,
                                             Y,
                                             map.Id);

                            map.ResetGridExpiry(this);
                        }
                }

                break;
        }
    }

    public void VisitAllGrids(IGridNotifier visitor)
    {
        for (uint x = 0; x < MapConst.MaxCells; ++x)
            for (uint y = 0; y < MapConst.MaxCells; ++y)
                GetGridCell(x, y).Visit(visitor);
    }

    public void VisitGrid(uint x, uint y, IGridNotifier visitor)
    {
        GetGridCell(x, y).Visit(visitor);
    }
}