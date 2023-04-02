// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class CreatureWorker : IGridNotifierCreature
{
    private readonly IDoWork<Creature> _doWork;
    private readonly PhaseShift _phaseShift;
    public CreatureWorker(WorldObject searcher, IDoWork<Creature> work, GridType gridType)
    {
        _phaseShift = searcher.Location.PhaseShift;
        _doWork = work;
        GridType = gridType;
    }

    public GridType GridType { get; set; }
    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var creature = objs[i];

            if (creature.Location.InSamePhase(_phaseShift))
                _doWork.Invoke(creature);
        }
    }
}