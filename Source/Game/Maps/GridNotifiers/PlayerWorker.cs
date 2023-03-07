// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class PlayerWorker : IGridNotifierPlayer
{
	readonly PhaseShift _phaseShift;
	readonly Action<Player> _action;

	public PlayerWorker(WorldObject searcher, Action<Player> action, GridType gridType)
	{
		_phaseShift = searcher.GetPhaseShift();
		_action       = action;
		GridType     = gridType;
	}

	public GridType GridType { get; set; }

	public void Visit(IList<Player> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var player = objs[i];

			if (player.InSamePhase(_phaseShift))
				_action.Invoke(player);
		}
	}
}