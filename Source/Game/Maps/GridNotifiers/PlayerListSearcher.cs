using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class PlayerListSearcher : IGridNotifierPlayer
{
    public GridType GridType { get; set; }

    readonly PhaseShift i_phaseShift;
    readonly List<Unit> i_objects;
    readonly ICheck<Player> i_check;

    public PlayerListSearcher(WorldObject searcher, List<Unit> objects, ICheck<Player> check, GridType gridType = GridType.World)
    {
        i_phaseShift = searcher.GetPhaseShift();
        i_objects = objects;
        i_check = check;
        GridType = gridType;
    }

    public PlayerListSearcher(PhaseShift phaseShift, List<Unit> objects, ICheck<Player> check, GridType gridType = GridType.World)
    {
        i_phaseShift = phaseShift;
        i_objects = objects;
        i_check = check;
        GridType = gridType;
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Player player = objs[i];
            if (player.InSamePhase(i_phaseShift))
                if (i_check.Invoke(player))
                    i_objects.Add(player);
        }
    }
}