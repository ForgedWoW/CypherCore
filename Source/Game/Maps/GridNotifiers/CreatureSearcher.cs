using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class CreatureSearcher : IGridNotifierCreature
{
    readonly PhaseShift i_phaseShift;
    Creature i_object;
    readonly ICheck<Creature> i_check;
    public GridType GridType { get; set; }

    public CreatureSearcher(WorldObject searcher, ICheck<Creature> check, GridType gridType)
    {
        i_phaseShift = searcher.GetPhaseShift();
        i_check = check;
        GridType = gridType;
    }

    public void Visit(IList<Creature> objs)
    {
        // already found
        if (i_object)
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            Creature creature = objs[i];
            if (!creature.InSamePhase(i_phaseShift))
                continue;

            if (i_check.Invoke(creature))
            {
                i_object = creature;
                return;
            }
        }
    }

    public Creature GetTarget() { return i_object; }
}