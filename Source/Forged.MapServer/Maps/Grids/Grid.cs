// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps.Grids;

public class Grid
{
	readonly uint _gridX;
	readonly uint _gridY;
	readonly GridInfo _gridInfo;
	readonly GridCell[][] _cells = new GridCell[MapConst.MaxCells][];
	readonly uint _gridId;
	GridState _gridState;
	bool _gridObjectDataLoaded;

	public Grid(uint id, uint x, uint y, long expiry, bool unload = true)
	{
		_gridId = id;
		_gridX = x;
		_gridY = y;
		_gridInfo = new GridInfo(expiry, unload);
		_gridState = GridState.Invalid;
		_gridObjectDataLoaded = false;

		for (uint xx = 0; xx < MapConst.MaxCells; ++xx)
		{
			_cells[xx] = new GridCell[MapConst.MaxCells];

			for (uint yy = 0; yy < MapConst.MaxCells; ++yy)
				_cells[xx][yy] = new GridCell();
		}
	}

	public Grid(Cell cell, uint expiry, bool unload = true) : this(cell.GetId(), cell.GetGridX(), cell.GetGridY(), expiry, unload) { }

	public GridCell GetGridCell(uint x, uint y)
	{
		return _cells[x][y];
	}

	public uint GetGridId()
	{
		return _gridId;
	}

	public GridState GetGridState()
	{
		return _gridState;
	}

	public void SetGridState(GridState s)
	{
		_gridState = s;
	}

	public uint GetX()
	{
		return _gridX;
	}

	public uint GetY()
	{
		return _gridY;
	}

	public bool IsGridObjectDataLoaded()
	{
		return _gridObjectDataLoaded;
	}

	public void SetGridObjectDataLoaded(bool pLoaded)
	{
		_gridObjectDataLoaded = pLoaded;
	}

	public GridInfo GetGridInfoRef()
	{
		return _gridInfo;
	}

	public void SetUnloadExplicitLock(bool on)
	{
		_gridInfo.SetUnloadExplicitLock(on);
	}

	public void IncUnloadActiveLock()
	{
		_gridInfo.IncUnloadActiveLock();
	}

	public void DecUnloadActiveLock()
	{
		_gridInfo.DecUnloadActiveLock();
	}

	public void ResetTimeTracker(long interval)
	{
		_gridInfo.ResetTimeTracker(interval);
	}

	public void Update(Map map, uint diff)
	{
		switch (GetGridState())
		{
			case GridState.Active:
				// Only check grid activity every (grid_expiry/10) ms, because it's really useless to do it every cycle
				GetGridInfoRef().UpdateTimeTracker(diff);

				if (GetGridInfoRef().GetTimeTracker().Passed)
				{
					if (GetWorldObjectCountInNGrid<Player>() == 0 && !map.ActiveObjectsNearGrid(this))
					{
						ObjectGridStoper worker = new(GridType.Grid);
						VisitAllGrids(worker);
						SetGridState(GridState.Idle);

						Log.Logger.Debug("Grid[{0}, {1}] on map {2} moved to IDLE state",
										GetX(),
										GetY(),
										map.Id);
					}
					else
					{
						map.ResetGridExpiry(this, 0.1f);
					}
				}

				break;
			case GridState.Idle:
				map.ResetGridExpiry(this);
				SetGridState(GridState.Removal);

				Log.Logger.Debug("Grid[{0}, {1}] on map {2} moved to REMOVAL state",
								GetX(),
								GetY(),
								map.Id);

				break;
			case GridState.Removal:
				if (!GetGridInfoRef().GetUnloadLock())
				{
					GetGridInfoRef().UpdateTimeTracker(diff);

					if (GetGridInfoRef().GetTimeTracker().Passed)
						if (!map.UnloadGrid(this, false))
						{
							Log.Logger.Debug("Grid[{0}, {1}] for map {2} differed unloading due to players or active objects nearby",
											GetX(),
											GetY(),
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

	public uint GetWorldObjectCountInNGrid<T>() where T : WorldObject
	{
		uint count = 0;

		for (uint x = 0; x < MapConst.MaxCells; ++x)
			for (uint y = 0; y < MapConst.MaxCells; ++y)
				count += _cells[x][y].GetWorldObjectCountInGrid<T>();

		return count;
	}
}