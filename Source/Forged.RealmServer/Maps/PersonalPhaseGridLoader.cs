// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Maps.Grids;
using Forged.RealmServer.Maps.Interfaces;

namespace Forged.RealmServer.Maps;

class PersonalPhaseGridLoader : ObjectGridLoaderBase, IGridNotifierCreature, IGridNotifierGameObject
{
	readonly ObjectGuid _phaseOwner;
	uint _phaseId;

	public GridType GridType { get; set; }

	public PersonalPhaseGridLoader(Grid grid, Map map, Cell cell, ObjectGuid phaseOwner, GridType gridType) : base(grid, map, cell)
	{
		_phaseId = 0;
		_phaseOwner = phaseOwner;
		GridType = gridType;
	}

	public void Visit(IList<Creature> objs)
	{
		var cellCoord = i_cell.GetCellCoord();
		var cell_guids = Global.ObjectMgr.GetCellPersonalObjectGuids(i_map.Id, i_map.DifficultyID, _phaseId, cellCoord.GetId());

		if (cell_guids != null)
			LoadHelper<Creature>(cell_guids.creatures, cellCoord, ref i_creatures, i_map, _phaseId, _phaseOwner);
	}

	public void Visit(IList<GameObject> objs)
	{
		var cellCoord = i_cell.GetCellCoord();
		var cell_guids = Global.ObjectMgr.GetCellPersonalObjectGuids(i_map.Id, i_map.DifficultyID, _phaseId, cellCoord.GetId());

		if (cell_guids != null)
			LoadHelper<GameObject>(cell_guids.gameobjects, cellCoord, ref i_gameObjects, i_map, _phaseId, _phaseOwner);
	}

	public void Load(uint phaseId)
	{
		_phaseId = phaseId;
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
}