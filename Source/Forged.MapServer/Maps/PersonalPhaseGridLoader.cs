// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps;

internal class PersonalPhaseGridLoader : ObjectGridLoaderBase, IGridNotifierCreature, IGridNotifierGameObject
{
    private readonly ObjectGuid _phaseOwner;
    private uint _phaseId;

    public PersonalPhaseGridLoader(Grid grid, Map map, Cell cell, ObjectGuid phaseOwner, GridType gridType) : base(grid, map, cell)
    {
        _phaseId = 0;
        _phaseOwner = phaseOwner;
        GridType = gridType;
    }

    public GridType GridType { get; set; }

    public void Visit(IList<Creature> objs)
    {
        var cellCoord = Cell.CellCoord;
        var cellGuids = Map.GameObjectManager.MapObjectCache.GetCellPersonalObjectGuids(Map.Id, Map.DifficultyID, _phaseId, cellCoord.GetId());

        if (cellGuids != null)
            Creatures = LoadHelper<Creature>(cellGuids.Creatures, cellCoord, Map, _phaseId, _phaseOwner);
    }

    public void Visit(IList<GameObject> objs)
    {
        var cellCoord = Cell.CellCoord;
        var cellGuids = Map.GameObjectManager.MapObjectCache.GetCellPersonalObjectGuids(Map.Id, Map.DifficultyID, _phaseId, cellCoord.GetId());

        if (cellGuids != null)
            GameObjects = LoadHelper<GameObject>(cellGuids.Gameobjects, cellCoord, Map, _phaseId, _phaseOwner);
    }

    public void Load(uint phaseId)
    {
        _phaseId = phaseId;
        Cell.Data.CellY = 0;

        for (uint x = 0; x < MapConst.MaxCells; ++x)
        {
            Cell.Data.CellX = x;

            for (uint y = 0; y < MapConst.MaxCells; ++y)
            {
                Cell.Data.CellY = y;

                //Load creatures and GameInfo objects
                Grid.VisitGrid(x, y, this);
            }
        }
    }
}