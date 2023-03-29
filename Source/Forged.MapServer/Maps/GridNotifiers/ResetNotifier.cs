// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class ResetNotifier : IGridNotifierPlayer, IGridNotifierCreature
{
    public GridType GridType { get; set; }

    public ResetNotifier(GridType gridType)
    {
        GridType = gridType;
    }

    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var creature = objs[i];
            creature.ResetAllNotifies();
        }
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            var player = objs[i];
            player.ResetAllNotifies();
        }
    }
}