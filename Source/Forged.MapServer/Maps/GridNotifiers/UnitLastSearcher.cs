// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
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

public class UnitLastSearcher : IGridNotifierPlayer, IGridNotifierCreature
{
    private readonly PhaseShift _phaseShift;
    private readonly ICheck<Unit> _check;
    private Unit _object;

    public GridType GridType { get; set; }

    public UnitLastSearcher(WorldObject searcher, ICheck<Unit> check, GridType gridType)
    {
        _phaseShift = searcher.PhaseShift;
        _check = check;
        GridType = gridType;
    }

    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var creature = objs[i];

            if (!creature.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(creature))
                _object = creature;
        }
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var player = objs[i];

            if (!player.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(player))
                _object = player;
        }
    }

    public Unit GetTarget()
    {
        return _object;
    }
}