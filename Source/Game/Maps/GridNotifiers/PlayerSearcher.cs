using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class PlayerSearcher : IGridNotifierPlayer
{
    public GridType GridType { get; set; }
    PhaseShift i_phaseShift;
    Player i_object;
    ICheck<Player> i_check;

    public PlayerSearcher(WorldObject searcher, ICheck<Player> check, GridType gridType)
    {
        i_phaseShift = searcher.GetPhaseShift();
        i_check = check;
        GridType = gridType;    
    }

    public void Visit(IList<Player> objs)
    {
        // already found
        if (i_object)
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            Player player = objs[i];
            if (!player.InSamePhase(i_phaseShift))
                continue;

            if (i_check.Invoke(player))
            {
                i_object = player;
                return;
            }
        }
    }

    public Player GetTarget() { return i_object; }
}