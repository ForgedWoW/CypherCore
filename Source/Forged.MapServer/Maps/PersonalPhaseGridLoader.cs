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
    public void Load(uint phaseId)
    {
        _phaseId = phaseId;
        ICell.Data.Celly = 0;

        for (uint x = 0; x < MapConst.MaxCells; ++x)
        {
            ICell.Data.Cellx = x;

            for (uint y = 0; y < MapConst.MaxCells; ++y)
            {
                ICell.Data.Celly = y;

                //Load creatures and GameInfo objects
                IGrid.VisitGrid(x, y, this);
            }
        }
    }

    public void Visit(IList<Creature> objs)
    {
        var cellCoord = ICell.GetCellCoord();
        var cellGuids = Global.ObjectMgr.GetCellPersonalObjectGuids(IMap.Id, IMap.DifficultyID, _phaseId, cellCoord.GetId());

        if (cellGuids != null)
            LoadHelper<Creature>(cellGuids.creatures, cellCoord, ref ICreatures, IMap, _phaseId, _phaseOwner);
    }

    public void Visit(IList<GameObject> objs)
    {
        var cellCoord = ICell.GetCellCoord();
        var cellGuids = Global.ObjectMgr.GetCellPersonalObjectGuids(IMap.Id, IMap.DifficultyID, _phaseId, cellCoord.GetId());

        if (cellGuids != null)
            LoadHelper<GameObject>(cellGuids.gameobjects, cellCoord, ref IGameObjects, IMap, _phaseId, _phaseOwner);
    }
}