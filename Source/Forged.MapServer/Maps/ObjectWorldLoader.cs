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
    private readonly Cell _iCell;
    private readonly Grid _iGrid;
    private readonly Map _iMap;

    public ObjectWorldLoader(ObjectGridLoaderBase gloader, GridType gridType)
    {
        _iCell = gloader.Cell;
        _iMap = gloader.Map;
        _iGrid = gloader.Grid;
        Corpses = gloader.Corpses;
        GridType = gridType;
    }

    public GridType GridType { get; set; }

    public uint Corpses { get; set; }

    public void Visit(IList<Corpse> objs)
    {
        var cellCoord = _iCell.CellCoord;
        var corpses = _iMap.GetCorpsesInCell(cellCoord.GetId());

        if (corpses == null)
            return;

        foreach (var corpse in corpses)
        {
            corpse.AddToWorld();
            var cell = _iGrid.GetGridCell(_iCell.Data.CellX, _iCell.Data.CellY);

            if (corpse.IsWorldObject())
            {
                _iMap.AddToGrid(corpse, new Cell(cellCoord, _iCell.GridDefines));
                cell.AddWorldObject(corpse);
            }
            else
                cell.AddGridObject(corpse);

            ++Corpses;
        }
    }
}