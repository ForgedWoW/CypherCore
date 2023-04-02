// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class CreatureListSearcher : IGridNotifierCreature
{
    internal PhaseShift _phaseShift;
    private readonly ICheck<Creature> _check;
    private readonly List<Creature> _objects;
    public CreatureListSearcher(WorldObject searcher, List<Creature> objects, ICheck<Creature> check, GridType gridType)
    {
        _phaseShift = searcher.Location.PhaseShift;
        _objects = objects;
        _check = check;
        GridType = gridType;
    }

    public GridType GridType { get; set; }
    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var creature = objs[i];

            if (creature.Location.InSamePhase(_phaseShift))
                if (_check.Invoke(creature))
                    _objects.Add(creature);
        }
    }
}