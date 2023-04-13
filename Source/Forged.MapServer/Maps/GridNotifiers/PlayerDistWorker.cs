// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class PlayerDistWorker : IGridNotifierPlayer
{
    private readonly float _dist;
    private readonly IDoWork<Player> _doWork;
    private readonly WorldObject _searcher;
    public PlayerDistWorker(WorldObject searcher, float dist, IDoWork<Player> work, GridType gridType)
    {
        _searcher = searcher;
        _dist = dist;
        _doWork = work;
        GridType = gridType;
    }

    public GridType GridType { get; set; }
    public void Visit(IList<Player> objs)
    {
        foreach (var player in objs)
        {
            if (player.Location.InSamePhase(_searcher) && player.Location.IsWithinDist(_searcher, _dist))
                _doWork.Invoke(player);
        }
    }
}