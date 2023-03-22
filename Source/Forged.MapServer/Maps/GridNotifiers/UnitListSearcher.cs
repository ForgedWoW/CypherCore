// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class UnitListSearcher : IGridNotifierCreature, IGridNotifierPlayer
{
	readonly PhaseShift _phaseShift;
	readonly List<Unit> _objects;
	readonly ICheck<Unit> _check;

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