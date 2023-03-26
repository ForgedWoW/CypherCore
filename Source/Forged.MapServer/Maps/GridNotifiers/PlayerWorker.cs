// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class PlayerWorker : IGridNotifierPlayer
{
    private readonly PhaseShift _phaseShift;
    private readonly Action<Player> _action;

	public GridType GridType { get; set; }

	public PlayerWorker(WorldObject searcher, Action<Player> action, GridType gridType)
	{
		_phaseShift = searcher.PhaseShift;
		_action = action;
		GridType = gridType;
	}

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