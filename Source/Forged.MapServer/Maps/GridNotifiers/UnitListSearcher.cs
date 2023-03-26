// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class UnitListSearcher : IGridNotifierCreature, IGridNotifierPlayer
{
    private readonly PhaseShift _phaseShift;
    private readonly List<Unit> _objects;
    private readonly ICheck<Unit> _check;

	public GridType GridType { get; set; }

	public UnitListSearcher(WorldObject searcher, List<Unit> objects, ICheck<Unit> check, GridType gridType)
	{
		_phaseShift = searcher.PhaseShift;
		_objects = objects;
		_check = check;
		GridType = gridType;
	}

	public void Visit(IList<Creature> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var creature = objs[i];

			if (creature.InSamePhase(_phaseShift))
				if (_check.Invoke(creature))
					_objects.Add(creature);
		}
	}

	public void Visit(IList<Player> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var player = objs[i];

			if (player.InSamePhase(_phaseShift))
				if (_check.Invoke(player))
					_objects.Add(player);
		}
	}
}