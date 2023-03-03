using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class PlayerRelocationNotifier : VisibleNotifier, IGridNotifierPlayer, IGridNotifierCreature
{
    public PlayerRelocationNotifier(Player player, GridType gridType) : base(player, gridType) { }

    public void Visit(IList<Player> objs)
    {
        Visit(objs.Cast<WorldObject>().ToList());

        for (var i = 0; i < objs.Count; ++i)
        {
            Player player = objs[i];
            vis_guids.Remove(player.GetGUID());

            i_player.UpdateVisibilityOf(player, i_data, i_visibleNow);

            if (player.seerView.IsNeedNotify(NotifyFlags.VisibilityChanged))
                continue;

            player.UpdateVisibilityOf(i_player);
        }
    }

    public void Visit(IList<Creature> objs)
    {
        Visit(objs.Cast<WorldObject>().ToList());

        bool relocated_for_ai = (i_player == i_player.seerView);

        for (var i = 0; i < objs.Count; ++i)
        {
            Creature creature = objs[i];
            vis_guids.Remove(creature.GetGUID());

            i_player.UpdateVisibilityOf(creature, i_data, i_visibleNow);

            if (relocated_for_ai && !creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
                NotifierHelpers.CreatureUnitRelocationWorker(creature, i_player);
        }
    }
}