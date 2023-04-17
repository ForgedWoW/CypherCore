﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class ResetNotifier : IGridNotifierPlayer, IGridNotifierCreature
{
    public ResetNotifier(GridType gridType)
    {
        GridType = gridType;
    }

    public GridType GridType { get; set; }

    public void Visit(IList<Creature> objs)
    {
        foreach (var creature in objs)
            creature.ResetAllNotifies();
    }

    public void Visit(IList<Player> objs)
    {
        foreach (var player in objs)
            player.ResetAllNotifies();
    }
}