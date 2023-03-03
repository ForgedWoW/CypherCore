using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class ResetNotifier : IGridNotifierPlayer, IGridNotifierCreature
{
    public GridType GridType { get; set; }

    public ResetNotifier(GridType gridType)
    {
        GridType = gridType;
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Player player = objs[i];
            player.ResetAllNotifies();
        }
    }

    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Creature creature = objs[i];
            creature.ResetAllNotifies();
        }
    }
}