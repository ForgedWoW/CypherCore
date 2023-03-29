// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps;

internal class ObjectWorldLoader : IGridNotifierCorpse
{
    public uint i_corpses;

    private readonly Cell i_cell;
    private readonly Map i_map;
    private readonly Grid i_grid;

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