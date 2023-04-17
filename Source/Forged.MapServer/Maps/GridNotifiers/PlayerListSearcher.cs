// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class PlayerListSearcher : IGridNotifierPlayer
{
    private readonly ICheck<Player> _check;
    private readonly List<Unit> _objects;
    private readonly PhaseShift _phaseShift;

    public PlayerListSearcher(WorldObject searcher, List<Unit> objects, ICheck<Player> check, GridType gridType = GridType.World)
    {
        _phaseShift = searcher.Location.PhaseShift;
        _objects = objects;
        _check = check;
        GridType = gridType;
    }

    public PlayerListSearcher(PhaseShift phaseShift, List<Unit> objects, ICheck<Player> check, GridType gridType = GridType.World)
    {
        _phaseShift = phaseShift;
        _objects = objects;
        _check = check;
        GridType = gridType;
    }

    public GridType GridType { get; set; }

    public void Visit(IList<Player> objs)
    {
        foreach (var player in objs)
        {
            if (player == null || !player.Location.InSamePhase(_phaseShift))
                continue;

            if (_check.Invoke(player))
                _objects.Add(player);
        }
    }
}