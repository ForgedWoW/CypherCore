// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class PlayerRelocationNotifier : VisibleNotifier, IGridNotifierPlayer, IGridNotifierCreature
{
    public PlayerRelocationNotifier(Player player, GridType gridType) : base(player, gridType) { }

    public void Visit(IList<Creature> objs)
    {
        Visit(objs.Cast<WorldObject>().ToList());

        var relocated_for_ai = Player == Player.SeerView;

        for (var i = 0; i < objs.Count; ++i)
        {
            var creature = objs[i];
            VisGuids.Remove(creature.GUID);

            Player.UpdateVisibilityOf(creature, Data, VisibleNow);

            if (relocated_for_ai && !creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
                NotifierHelpers.CreatureUnitRelocationWorker(creature, Player);
        }
    }

    public void Visit(IList<Player> objs)
    {
        Visit(objs.Cast<WorldObject>().ToList());

        for (var i = 0; i < objs.Count; ++i)
        {
            var player = objs[i];
            VisGuids.Remove(player.GUID);

            Player.UpdateVisibilityOf(player, Data, VisibleNow);

            if (player.SeerView.IsNeedNotify(NotifyFlags.VisibilityChanged))
                continue;

            player.UpdateVisibilityOf(Player);
        }
    }
}