// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Maps.Grids;
using Forged.RealmServer.Maps.Interfaces;

namespace Forged.RealmServer.Maps;

class ObjectWorldLoader : IGridNotifierCorpse
{
	public uint i_corpses;

	readonly Cell i_cell;
	readonly Map i_map;
	readonly Grid i_grid;

	public GridType GridType { get; set; }

	public ObjectWorldLoader(ObjectGridLoaderBase gloader, GridType gridType)
	{
		i_cell = gloader.i_cell;
		i_map = gloader.i_map;
		i_grid = gloader.i_grid;
		i_corpses = gloader.i_corpses;
		GridType = gridType;
	}

	public void Visit(IList<Corpse> objs)
	{
		var cellCoord = i_cell.GetCellCoord();
		var corpses = i_map.GetCorpsesInCell(cellCoord.GetId());

		if (corpses != null)
			foreach (var corpse in corpses)
			{
				corpse.AddToWorld();
				var cell = i_grid.GetGridCell(i_cell.GetCellX(), i_cell.GetCellY());

				if (corpse.IsWorldObject())
				{
					i_map.AddToGrid(corpse, new Cell(cellCoord));
					cell.AddWorldObject(corpse);
				}
				else
				{
					cell.AddGridObject(corpse);
				}

				++i_corpses;
			}
	}
}