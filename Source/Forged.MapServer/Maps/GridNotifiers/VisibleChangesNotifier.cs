// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class VisibleChangesNotifier : IGridNotifierCreature, IGridNotifierPlayer, IGridNotifierDynamicObject
{
    private readonly ICollection<WorldObject> _objects;

    public VisibleChangesNotifier(ICollection<WorldObject> objects, GridType gridType)
    {
        _objects = objects;
        GridType = gridType;
    }

    public GridType GridType { get; set; }

    public void Visit(IList<Creature> objs)
    {
        foreach (var creature in objs)
        {
            if (creature == null) continue;

            foreach (var visionPlayer in creature.GetSharedVisionList().Where(visionPlayer => visionPlayer.SeerView == creature))
                visionPlayer.UpdateVisibilityOf(_objects);
        }
    }

    public void Visit(IList<DynamicObject> objs)
    {
        foreach (var dynamicObject in objs)
        {
            var pl = dynamicObject.Caster?.AsPlayer;

            if (pl == null)
                continue;

            if (pl.SeerView == dynamicObject)
                pl.UpdateVisibilityOf(_objects);
        }
    }

    public void Visit(IList<Player> objs)
    {
        foreach (var player in objs)
        {
            if (player == null) continue;

            player.UpdateVisibilityOf(_objects);

            foreach (var visionPlayer in player.GetSharedVisionList().Where(visionPlayer => visionPlayer.SeerView == player))
                visionPlayer.UpdateVisibilityOf(_objects);
        }
    }
}