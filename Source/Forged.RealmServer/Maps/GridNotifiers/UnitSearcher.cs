// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Maps.Interfaces;

namespace Forged.RealmServer.Maps;

public class UnitSearcher : IGridNotifierPlayer, IGridNotifierCreature
{
	readonly PhaseShift _phaseShift;
	readonly ICheck<Unit> _check;
	Unit _object;

	public GridType GridType { get; set; }

	public UnitSearcher(WorldObject searcher, ICheck<Unit> check, GridType gridType)
	{
		_phaseShift = searcher.PhaseShift;
		_check = check;
		GridType = gridType;
	}

	public void Visit(IList<Creature> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var creature = objs[i];

			if (!creature.InSamePhase(_phaseShift))
				continue;

			if (_check.Invoke(creature))
			{
				_object = creature;

				return;
			}
		}
	}

	public void Visit(IList<Player> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var player = objs[i];

			if (!player.InSamePhase(_phaseShift))
				continue;

			if (_check.Invoke(player))
			{
				_object = player;

				return;
			}
		}
	}

	public Unit GetTarget()
	{
		return _object;
	}
}