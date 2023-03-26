// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class PlayerDistWorker : IGridNotifierPlayer
{
    private readonly WorldObject _searcher;
    private readonly float _dist;
    private readonly IDoWork<Player> _doWork;

	public GridType GridType { get; set; }

	public PlayerDistWorker(WorldObject searcher, float dist, IDoWork<Player> work, GridType gridType)
	{
		_searcher = searcher;
		_dist = dist;
		_doWork = work;
		GridType = gridType;
	}

	public void Visit(IList<Player> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var player = objs[i];

			if (player.InSamePhase(_searcher) && player.IsWithinDist(_searcher, _dist))
				_doWork.Invoke(player);
		}
	}
}