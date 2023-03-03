using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class DelayedUnitRelocation : IGridNotifierCreature, IGridNotifierPlayer
{
    public GridType GridType { get; set; }
    public DelayedUnitRelocation(Cell c, CellCoord pair, Map map, float radius, GridType gridType)
    {
        i_map = map;
        cell = c;
        p = pair;
        i_radius = radius;
        GridType = gridType;
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Player player = objs[i];
            WorldObject viewPoint = player.seerView;

            if (!viewPoint.IsNeedNotify(NotifyFlags.VisibilityChanged))
                continue;

            if (player != viewPoint && !viewPoint.IsPositionValid())
                continue;

            var relocate = new PlayerRelocationNotifier(player, GridType.All);
            Cell.VisitGrid(viewPoint, relocate, i_radius, false);

            relocate.SendToSelf();
        }
    }

    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Creature creature = objs[i];
            if (!creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
                continue;

            CreatureRelocationNotifier relocate = new(creature, GridType.All);

            cell.Visit(p, relocate, i_map, creature, i_radius);
        }
    }

    readonly Map i_map;
    readonly Cell cell;
    readonly CellCoord p;
    readonly float i_radius;
}