﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class UnitSearcher : IGridNotifierPlayer, IGridNotifierCreature
{
    private readonly ICheck<Unit> _check;
    private readonly PhaseShift _phaseShift;
    private Unit _object;

    public UnitSearcher(WorldObject searcher, ICheck<Unit> check, GridType gridType)
    {
        _phaseShift = searcher.Location.PhaseShift;
        _check = check;
        GridType = gridType;
    }

    public GridType GridType { get; set; }
    public Unit GetTarget()
    {
        return _object;
    }

    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var creature = objs[i];

            if (!creature.Location.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(creature))
            {
                _object = creature;

                return;
            }
        }
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var player = objs[i];

            if (!player.Location.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(player))
            {
                _object = player;

                return;
            }
        }
    }
}