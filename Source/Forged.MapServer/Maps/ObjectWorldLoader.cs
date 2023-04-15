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
    public uint ICorpses;

    private readonly Cell _iCell;
    private readonly Grid _iGrid;
    private readonly Map _iMap;
    public ObjectWorldLoader(ObjectGridLoaderBase gloader, GridType gridType)
    {
        _iCell = gloader.ICell;
        _iMap = gloader.IMap;
        _iGrid = gloader.IGrid;
        ICorpses = gloader.ICorpses;
        GridType = gridType;
    }

    public GridType GridType { get; set; }
    public void Visit(IList<Corpse> objs)
    {
        var cellCoord = _iCell.GetCellCoord();
        var corpses = _iMap.GetCorpsesInCell(cellCoord.GetId());

        if (corpses != null)
            foreach (var corpse in corpses)
            {
                corpse.AddToWorld();
                var cell = _iGrid.GetGridCell(_iCell.GetCellX(), _iCell.GetCellY());

                if (corpse.IsWorldObject())
                {
                    _iMap.AddToGrid(corpse, new Cell(cellCoord));
                    cell.AddWorldObject(corpse);
                }
                else
                {
                    cell.AddGridObject(corpse);
                }

                ++ICorpses;
            }
    }
}