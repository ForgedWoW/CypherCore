using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class CreatureRelocationNotifier : IGridNotifierCreature, IGridNotifierPlayer
{
    public GridType GridType { get; set; }
    public CreatureRelocationNotifier(Creature c, GridType gridType)
    {
        i_creature = c;
        GridType = gridType;
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Player player = objs[i];
            if (!player.seerView.IsNeedNotify(NotifyFlags.VisibilityChanged))
                player.UpdateVisibilityOf(i_creature);

            NotifierHelpers.CreatureUnitRelocationWorker(i_creature, player);
        }
    }

    public void Visit(IList<Creature> objs)
    {
        if (!i_creature.IsAlive())
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            Creature creature = objs[i];
            NotifierHelpers.CreatureUnitRelocationWorker(i_creature, creature);

            if (!creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
                NotifierHelpers.CreatureUnitRelocationWorker(creature, i_creature);
        }
    }

    Creature i_creature;
}