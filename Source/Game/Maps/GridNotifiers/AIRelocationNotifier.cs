using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class AIRelocationNotifier : IGridNotifierCreature
{
    public GridType GridType { get; set; }
    public AIRelocationNotifier(Unit unit, GridType gridType)
    {
        i_unit = unit;
        isCreature = unit.IsTypeId(TypeId.Unit);
        GridType = gridType;
    }

    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Creature creature = objs[i];
            NotifierHelpers.CreatureUnitRelocationWorker(creature, i_unit);
            if (isCreature)
                NotifierHelpers.CreatureUnitRelocationWorker(i_unit.ToCreature(), creature);
        }
    }

    Unit i_unit;
    bool isCreature;
}