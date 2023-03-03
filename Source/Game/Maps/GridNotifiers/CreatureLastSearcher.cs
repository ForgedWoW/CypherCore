using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class CreatureLastSearcher : IGridNotifierCreature
{
    internal PhaseShift i_phaseShift;
    Creature i_object;
    ICheck<Creature> i_check;
    public GridType GridType { get; set; }

    public CreatureLastSearcher(WorldObject searcher, ICheck<Creature> check, GridType gridType)
    {
        i_phaseShift = searcher.GetPhaseShift();
        i_check = check;
        GridType = gridType;
    }

    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Creature creature = objs[i];
            if (!creature.InSamePhase(i_phaseShift))
                continue;

            if (i_check.Invoke(creature))
                i_object = creature;
        }
    }

    public Creature GetTarget() { return i_object; }
}