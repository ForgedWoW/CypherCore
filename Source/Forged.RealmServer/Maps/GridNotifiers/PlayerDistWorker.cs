// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Maps.Interfaces;

namespace Forged.RealmServer.Maps;

public class PlayerDistWorker : IGridNotifierPlayer
{
	readonly WorldObject _searcher;
	readonly float _dist;
	readonly IDoWork<Player> _doWork;

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