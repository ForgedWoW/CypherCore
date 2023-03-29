// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class PlayerSearcher : IGridNotifierPlayer
{
    private readonly PhaseShift _phaseShift;
    private readonly ICheck<Player> _check;
    private Player _object;

    public GridType GridType { get; set; }

    public PlayerSearcher(WorldObject searcher, ICheck<Player> check, GridType gridType)
    {
        _phaseShift = searcher.Location.PhaseShift;
        _check = check;
        GridType = gridType;
    }

    public void Visit(IList<Player> objs)
    {
        // already found
        if (_object)
            return;

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

    public Player GetTarget()
    {
        return _object;
    }
}