// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class GameObjectWorker : IGridNotifierGameObject
{
    private readonly IDoWork<GameObject> _doWork;
    private readonly PhaseShift _phaseShift;

    public GameObjectWorker(WorldObject searcher, IDoWork<GameObject> work, GridType gridType)
    {
        _phaseShift = searcher.Location.PhaseShift;
        _doWork = work;
        GridType = gridType;
    }

    public GridType GridType { get; set; }

    public void Visit(IList<GameObject> objs)
    {
        foreach (var gameObject in objs)
            if (gameObject.Location.InSamePhase(_phaseShift))
                _doWork.Invoke(gameObject);
    }
}