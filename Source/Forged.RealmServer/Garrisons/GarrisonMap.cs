// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Forged.RealmServer.Maps;
using Forged.RealmServer.Maps.Grids;
using Forged.RealmServer.Maps.Interfaces;

namespace Forged.RealmServer.Garrisons;

class GarrisonMap : Map
{
	readonly ObjectGuid _owner;
	Player _loadingPlayer; // @workaround Player is not registered in ObjectAccessor during login

	public GarrisonMap(uint id, long expiry, uint instanceId, ObjectGuid owner) : base(id, expiry, instanceId, Difficulty.Normal)
	{
		_owner = owner;
		InitVisibilityDistance();
	}

	public override void LoadGridObjects(Grid grid, Cell cell)
	{
		base.LoadGridObjects(grid, cell);

		GarrisonGridLoader loader = new(grid, this, cell);
		loader.LoadN();
	}

	public Garrison GetGarrison()
	{
		if (_loadingPlayer)
			return _loadingPlayer.Garrison;

		var owner = Global.ObjAccessor.FindConnectedPlayer(_owner);

		if (owner)
			return owner.Garrison;

		return null;
	}

	public override void InitVisibilityDistance()
	{
		//init visibility distance for instances
		VisibleDistance = Global.WorldMgr.MaxVisibleDistanceInInstances;
		VisibilityNotifyPeriod = Global.WorldMgr.VisibilityNotifyPeriodInInstances;
	}

	public override bool AddPlayerToMap(Player player, bool initPlayer = true)
	{
		if (player.GUID == _owner)
			_loadingPlayer = player;

		var result = base.AddPlayerToMap(player, initPlayer);

		if (player.GUID == _owner)
			_loadingPlayer = null;

		return result;
	}
}

class GarrisonGridLoader : IGridNotifierGameObject
{
	readonly Cell i_cell;
	readonly Grid i_grid;
	readonly GarrisonMap i_map;
	readonly Garrison i_garrison;
	readonly uint i_creatures;
	uint i_gameObjects;
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

					//Load creatures and game objects
					i_grid.VisitGrid(x, y, this);
				}
			}
		}

		Log.outDebug(LogFilter.Maps, "{0} GameObjects and {1} Creatures loaded for grid {2} on map {3}", i_gameObjects, i_creatures, i_grid.GetGridId(), i_map.Id);
	}
}