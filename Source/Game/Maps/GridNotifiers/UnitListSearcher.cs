using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class UnitListSearcher : IGridNotifierCreature, IGridNotifierPlayer
{
    readonly PhaseShift i_phaseShift;
    readonly List<Unit> i_objects;
    readonly ICheck<Unit> i_check;
    public GridType GridType { get; set; }

    public UnitListSearcher(WorldObject searcher, List<Unit> objects, ICheck<Unit> check, GridType gridType)
    {
        i_phaseShift = searcher.GetPhaseShift();
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

    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Creature creature = objs[i];
            if (creature.InSamePhase(i_phaseShift))
                if (i_check.Invoke(creature))
                    i_objects.Add(creature);
        }
    }
}