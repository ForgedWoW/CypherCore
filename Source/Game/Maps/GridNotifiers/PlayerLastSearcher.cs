using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class PlayerLastSearcher : IGridNotifierPlayer
{
    public GridType GridType { get; set; }
    PhaseShift i_phaseShift;
    Player i_object;
    ICheck<Player> i_check;

    public PlayerLastSearcher(WorldObject searcher, ICheck<Player> check, GridType gridType)
    {
        i_phaseShift = searcher.GetPhaseShift();
        i_check = check;
        GridType = gridType;
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Player player = objs[i];
            if (!player.InSamePhase(i_phaseShift))
                continue;

            if (i_check.Invoke(player))
                i_object = player;
        }
    }

    public Player GetTarget() { return i_object; }
}