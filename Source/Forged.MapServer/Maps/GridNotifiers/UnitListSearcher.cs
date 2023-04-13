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

public class UnitListSearcher : IGridNotifierCreature, IGridNotifierPlayer
{
    private readonly ICheck<Unit> _check;
    private readonly List<Unit> _objects;
    private readonly PhaseShift _phaseShift;
    public UnitListSearcher(WorldObject searcher, List<Unit> objects, ICheck<Unit> check, GridType gridType)
    {
        _phaseShift = searcher.Location.PhaseShift;
        _objects = objects;
        _check = check;
        GridType = gridType;
    }

    public GridType GridType { get; set; }
    public void Visit(IList<Creature> objs)
    {
        foreach (var creature in objs)
        {
            if (!creature.Location.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(creature))
                _objects.Add(creature);
        }
    }

    public void Visit(IList<Player> objs)
    {
        foreach (var player in objs)
        {
            if (!player.Location.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(player))
                _objects.Add(player);
        }
    }
}