﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class CreatureSearcher : IGridNotifierCreature
{
    private readonly ICheck<Creature> _check;
    private readonly PhaseShift _phaseShift;
    private Creature _object;

    public CreatureSearcher(WorldObject searcher, ICheck<Creature> check, GridType gridType)
    {
        _phaseShift = searcher.Location.PhaseShift;
        _check = check;
        GridType = gridType;
    }

    public GridType GridType { get; set; }

    public void Visit(IList<Creature> objs)
    {
        // already found
        if (_object != null)
            return;

        foreach (var creature in objs)
        {
            if (!creature.Location.InSamePhase(_phaseShift))
                continue;

            if (!_check.Invoke(creature))
                continue;

            _object = creature;

            return;
        }
    }

    public Creature GetTarget()
    {
        return _object;
    }
}