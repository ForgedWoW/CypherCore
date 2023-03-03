using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class CreatureListSearcher : IGridNotifierCreature
{
    internal PhaseShift i_phaseShift;
    List<Creature> i_objects;
    ICheck<Creature> i_check;
    public GridType GridType { get; set; }


    public CreatureListSearcher(WorldObject searcher, List<Creature> objects, ICheck<Creature> check, GridType gridType)
    {
        i_phaseShift = searcher.GetPhaseShift();
        i_objects = objects;
        i_check = check;
        GridType = gridType;
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