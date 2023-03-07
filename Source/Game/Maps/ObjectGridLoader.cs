// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Grids;
using Game.Maps.Interfaces;

namespace Game.Maps;

class ObjectGridLoader : ObjectGridLoaderBase, IGridNotifierGameObject, IGridNotifierCreature, IGridNotifierAreaTrigger
{
	public ObjectGridLoader(Grid grid, Map map, Cell cell, GridType gridType) : base(grid, map, cell)
	{
		GridType = gridType;
	}

	public void Visit(IList<AreaTrigger> objs)
	{
		var cellCoord = i_cell.GetCellCoord();
		var areaTriggers = Global.AreaTriggerDataStorage.GetAreaTriggersForMapAndCell(i_map.GetId(), cellCoord.GetId());

		if (areaTriggers == null || areaTriggers.Empty())
			return;

		LoadHelper<AreaTrigger>(areaTriggers, cellCoord, ref i_areaTriggers, i_map);
	}

	public void Visit(IList<Creature> objs)
	{
		var cellCoord = i_cell.GetCellCoord();
		var cellguids = Global.ObjectMgr.GetCellObjectGuids(i_map.GetId(), i_map.GetDifficultyID(), cellCoord.GetId());

		if (cellguids == null || cellguids.creatures.Empty())
			return;

		LoadHelper<Creature>(cellguids.creatures, cellCoord, ref i_creatures, i_map);
	}

	public GridType GridType { get; set; }

	public void Visit(IList<GameObject> objs)
	{
		var cellCoord = i_cell.GetCellCoord();
		var cellguids = Global.ObjectMgr.GetCellObjectGuids(i_map.GetId(), i_map.GetDifficultyID(), cellCoord.GetId());

		if (cellguids == null || cellguids.gameobjects.Empty())
			return;

		LoadHelper<GameObject>(cellguids.gameobjects, cellCoord, ref i_gameObjects, i_map);
	}

	public void LoadN()
	{
		i_creatures        = 0;
		i_gameObjects      = 0;
		i_corpses          = 0;
		i_cell.Data.Celly = 0;

		for (uint x = 0; x < MapConst.MaxCells; ++x)
		{
			i_cell.Data.Cellx = x;

			for (uint y = 0; y < MapConst.MaxCells; ++y)
			{
				i_cell.Data.Celly = y;

				i_grid.VisitGrid(x, y, this);

				ObjectWorldLoader worker = new(this, GridType.World);
				i_grid.VisitGrid(x, y, worker);
			}
		}

		Log.outDebug(LogFilter.Maps, $"{i_gameObjects} GameObjects, {i_creatures} Creatures, {i_areaTriggers} AreaTrriggers and {i_corpses} Corpses/Bones loaded for grid {i_grid.GetGridId()} on map {i_map.GetId()}");
	}
}

//Stop the creatures before unloading the NGrid

//Move the foreign creatures back to respawn positions before unloading the NGrid

//Clean up and remove from world

//Delete objects before deleting NGrid