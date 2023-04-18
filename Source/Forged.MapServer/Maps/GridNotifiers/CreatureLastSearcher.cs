// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class CreatureLastSearcher : IGridNotifierCreature
{
    internal PhaseShift PhaseShift;
    private readonly ICheck<Creature> _check;

    public CreatureLastSearcher(WorldObject searcher, ICheck<Creature> check, GridType gridType)
    {
        PhaseShift = searcher.Location.PhaseShift;
        _check = check;
        GridType = gridType;
    }

    public GridType GridType { get; set; }
    public Creature Target { get; private set; }

    public void Visit(IList<Creature> objs)
    {
        foreach (var creature in objs)
        {
            if (!creature.Location.InSamePhase(PhaseShift))
                continue;

            if (_check.Invoke(creature))
                Target = creature;
        }
    }
}