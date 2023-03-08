// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Grids;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class DelayedUnitRelocation : IGridNotifierCreature, IGridNotifierPlayer
{
	readonly Map _map;
	readonly Cell _cell;
	readonly CellCoord _p;
	readonly float _radius;

	public DelayedUnitRelocation(Cell c, CellCoord pair, Map map, float radius, GridType gridType)
	{
		_map    = map;
		_cell     = c;
		_p        = pair;
		_radius = radius;
		GridType = gridType;
	}

	public GridType GridType { get; set; }

	public void Visit(IList<Creature> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var creature = objs[i];

			if (!creature.IsNeedNotify(NotifyFlags.VisibilityChanged))
				continue;

			CreatureRelocationNotifier relocate = new(creature, GridType.All);

			_cell.Visit(_p, relocate, _map, creature, _radius);
		}
	}

	public void Visit(IList<Player> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var player = objs[i];
			var viewPoint = player.SeerView;

			if (!viewPoint.IsNeedNotify(NotifyFlags.VisibilityChanged))
				continue;

			if (player != viewPoint && !viewPoint.Location.IsPositionValid())
				continue;

			var relocate = new PlayerRelocationNotifier(player, GridType.All);
			Cell.VisitGrid(viewPoint, relocate, _radius, false);

			relocate.SendToSelf();
		}
	}
}