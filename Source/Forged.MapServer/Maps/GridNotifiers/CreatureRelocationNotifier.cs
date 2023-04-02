// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class CreatureRelocationNotifier : IGridNotifierCreature, IGridNotifierPlayer
{
    private readonly Creature _creature;

    public CreatureRelocationNotifier(Creature c, GridType gridType)
    {
        _creature = c;
        GridType = gridType;
    }

    public GridType GridType { get; set; }
    public void Visit(IList<Creature> objs)
    {
        if (!_creature.IsAlive)
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            var creature = objs[i];
            NotifierHelpers.CreatureUnitRelocationWorker(_creature, creature);

            if (!creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
                NotifierHelpers.CreatureUnitRelocationWorker(creature, _creature);
        }
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var player = objs[i];

            if (!player.SeerView.IsNeedNotify(NotifyFlags.VisibilityChanged))
                player.UpdateVisibilityOf(_creature);

            NotifierHelpers.CreatureUnitRelocationWorker(_creature, player);
        }
    }
}