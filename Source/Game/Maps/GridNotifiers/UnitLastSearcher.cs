// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class UnitLastSearcher : IGridNotifierPlayer, IGridNotifierCreature
{
	readonly PhaseShift _phaseShift;
	readonly ICheck<Unit> _check;
	Unit _object;

	public UnitLastSearcher(WorldObject searcher, ICheck<Unit> check, GridType gridType)
	{
		_phaseShift = searcher.GetPhaseShift();
		_check      = check;
		GridType     = gridType;
	}

	public void Visit(IList<Creature> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var creature = objs[i];

			if (!creature.InSamePhase(_phaseShift))
				continue;

			if (_check.Invoke(creature))
				_object = creature;
		}
	}

	public GridType GridType { get; set; }

	public void Visit(IList<Player> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var player = objs[i];

			if (!player.InSamePhase(_phaseShift))
				continue;

			if (_check.Invoke(player))
				_object = player;
		}
	}

	public Unit GetTarget()
	{
		return _object;
	}
}