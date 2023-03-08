// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class PlayerSearcher : IGridNotifierPlayer
{
	readonly PhaseShift _phaseShift;
	readonly ICheck<Player> _check;
	Player _object;

	public GridType GridType { get; set; }

	public PlayerSearcher(WorldObject searcher, ICheck<Player> check, GridType gridType)
	{
		_phaseShift = searcher.GetPhaseShift();
		_check = check;
		GridType = gridType;
	}

	public void Visit(IList<Player> objs)
	{
		// already found
		if (_object)
			return;

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

	public Player GetTarget()
	{
		return _object;
	}
}