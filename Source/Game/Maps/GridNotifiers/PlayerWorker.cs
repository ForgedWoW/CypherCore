using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class PlayerWorker : IGridNotifierPlayer
{
    public GridType GridType { get; set; }
    PhaseShift i_phaseShift;
    Action<Player> action;

    public PlayerWorker(WorldObject searcher, Action<Player> _action, GridType gridType)
    {
        i_phaseShift = searcher.GetPhaseShift();
        action = _action;
        GridType = gridType;
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Player player = objs[i];
            if (player.InSamePhase(i_phaseShift))
                action.Invoke(player);
        }
    }
}