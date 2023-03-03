using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class CreatureWorker : IGridNotifierCreature
{
    public GridType GridType { get; set; }
    PhaseShift i_phaseShift;
    IDoWork<Creature> Do;

    public CreatureWorker(WorldObject searcher, IDoWork<Creature> _Do, GridType gridType)
    {
        i_phaseShift = searcher.GetPhaseShift();
        Do = _Do;
        GridType = gridType;
    }

    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Creature creature = objs[i];
            if (creature.InSamePhase(i_phaseShift))
                Do.Invoke(creature);
        }
    }
}