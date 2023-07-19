// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class DelayedUnitRelocation : IGridNotifierCreature, IGridNotifierPlayer
{
    private readonly Cell _cell;
    private readonly Map _map;
    private readonly ObjectAccessor _objectAccessor;
    private readonly CellCoord _p;
    private readonly float _radius;

    public DelayedUnitRelocation(Cell c, CellCoord pair, Map map, float radius, GridType gridType, ObjectAccessor objectAccessor)
    {
        _map = map;
        _cell = c;
        _p = pair;
        _radius = radius;
        _objectAccessor = objectAccessor;
        GridType = gridType;
    }

    public GridType GridType { get; set; }

    public void Visit(IList<Creature> objs)
    {
        foreach (var creature in objs)
        {
            if (!creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
                continue;

            CreatureRelocationNotifier relocate = new(creature, GridType.All);

            _cell.Visit(_p, relocate, _map, creature, _radius);
        }
    }

    public void Visit(IList<Player> objs)
    {
        foreach (var player in objs)
        {
            var viewPoint = player.SeerView;

            if (!viewPoint.IsNeedNotify(NotifyFlags.VisibilityChanged))
                continue;

            if (player != viewPoint && !viewPoint.Location.IsPositionValid)
                continue;

            var relocate = new PlayerRelocationNotifier(player, GridType.All, _objectAccessor);
            player.CellCalculator.VisitGrid(viewPoint, relocate, _radius, false);

            relocate.SendToSelf();
        }
    }
}