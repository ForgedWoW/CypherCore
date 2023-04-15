// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
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
    public ObjectGridLoader(Grid grid, Map map, Cell cell, GridType gridType) : base(grid, map, cell)
    {
        GridType = gridType;
    }

    public GridType GridType { get; set; }
    public void LoadN()
    {
        ICreatures = 0;
        IGameObjects = 0;
        ICorpses = 0;
        ICell.Data.Celly = 0;

        for (uint x = 0; x < MapConst.MaxCells; ++x)
        {
            ICell.Data.Cellx = x;

            for (uint y = 0; y < MapConst.MaxCells; ++y)
            {
                ICell.Data.Celly = y;

                IGrid.VisitGrid(x, y, this);

                ObjectWorldLoader worker = new(this, GridType.World);
                IGrid.VisitGrid(x, y, worker);
            }
        }

        Log.Logger.Debug($"{IGameObjects} GameObjects, {ICreatures} Creatures, {IAreaTriggers} AreaTrriggers and {ICorpses} Corpses/Bones loaded for grid {IGrid.GridId} on map {IMap.Id}");
    }

    public void Visit(IList<AreaTrigger> objs)
    {
        var cellCoord = ICell.GetCellCoord();
        var areaTriggers = Global.AreaTriggerDataStorage.GetAreaTriggersForMapAndCell(IMap.Id, cellCoord.GetId());

        if (areaTriggers == null || areaTriggers.Empty())
            return;

        LoadHelper<AreaTrigger>(areaTriggers, cellCoord, ref IAreaTriggers, IMap);
    }

    public void Visit(IList<Creature> objs)
    {
        var cellCoord = ICell.GetCellCoord();
        var cellguids = Global.ObjectMgr.GetCellObjectGuids(IMap.Id, IMap.DifficultyID, cellCoord.GetId());

        if (cellguids == null || cellguids.creatures.Empty())
            return;

        LoadHelper<Creature>(cellguids.creatures, cellCoord, ref ICreatures, IMap);
    }

    public void Visit(IList<GameObject> objs)
    {
        var cellCoord = ICell.GetCellCoord();
        var cellguids = Global.ObjectMgr.GetCellObjectGuids(IMap.Id, IMap.DifficultyID, cellCoord.GetId());

        if (cellguids == null || cellguids.gameobjects.Empty())
            return;

        LoadHelper<GameObject>(cellguids.gameobjects, cellCoord, ref IGameObjects, IMap);
    }
}

//Stop the creatures before unloading the NGrid

//Move the foreign creatures back to respawn positions before unloading the NGrid

//Clean up and remove from world

//Delete objects before deleting NGrid