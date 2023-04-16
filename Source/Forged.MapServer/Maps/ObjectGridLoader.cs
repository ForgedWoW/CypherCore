// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Maps;

internal class ObjectGridLoader : ObjectGridLoaderBase, IGridNotifierGameObject, IGridNotifierCreature, IGridNotifierAreaTrigger
{
    private readonly AreaTriggerDataStorage _areaTriggerDataStorage;
    public ObjectGridLoader(Grid grid, Map map, Cell cell, GridType gridType) : base(grid, map, cell)
    {
        GridType = gridType;
        _areaTriggerDataStorage = map.ClassFactory.Resolve<AreaTriggerDataStorage>();
    }

    public GridType GridType { get; set; }
    public void LoadN()
    {
        Creatures = 0;
        GameObjects = 0;
        Corpses = 0;
        Cell.Data.CellY = 0;

        for (uint x = 0; x < MapConst.MaxCells; ++x)
        {
            Cell.Data.CellX = x;

            for (uint y = 0; y < MapConst.MaxCells; ++y)
            {
                Cell.Data.CellY = y;

                Grid.VisitGrid(x, y, this);

                ObjectWorldLoader worker = new(this, GridType.World);
                Grid.VisitGrid(x, y, worker);
            }
        }

        Log.Logger.Debug($"{GameObjects} GameObjects, {Creatures} Creatures, {AreaTriggers} AreaTrriggers and {Corpses} Corpses/Bones loaded for grid {Grid.GridId} on map {Map.Id}");
    }

    public void Visit(IList<AreaTrigger> objs)
    {
        var cellCoord = Cell.CellCoord;
        var areaTriggers = _areaTriggerDataStorage.GetAreaTriggersForMapAndCell(Map.Id, cellCoord.GetId());

        if (areaTriggers == null || areaTriggers.Empty())
            return;

        AreaTriggers = LoadHelper<AreaTrigger>(areaTriggers, cellCoord, Map);
    }

    public void Visit(IList<Creature> objs)
    {
        var cellCoord = Cell.CellCoord;
        var cellguids = Map.GameObjectManager.GetCellObjectGuids(Map.Id, Map.DifficultyID, cellCoord.GetId());

        if (cellguids == null || cellguids.Creatures.Empty())
            return;

        Creatures = LoadHelper<Creature>(cellguids.Creatures, cellCoord, Map);
    }

    public void Visit(IList<GameObject> objs)
    {
        var cellCoord = Cell.CellCoord;
        var cellguids = Map.GameObjectManager.GetCellObjectGuids(Map.Id, Map.DifficultyID, cellCoord.GetId());

        if (cellguids == null || cellguids.Gameobjects.Empty())
            return;

        GameObjects = LoadHelper<GameObject>(cellguids.Gameobjects, cellCoord, Map);
    }
}