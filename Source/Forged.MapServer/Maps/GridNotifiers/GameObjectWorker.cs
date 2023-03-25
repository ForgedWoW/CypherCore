// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class GameObjectWorker : IGridNotifierGameObject
{
	readonly PhaseShift _phaseShift;
	readonly IDoWork<GameObject> _doWork;

	public GridType GridType { get; set; }

	public GameObjectWorker(WorldObject searcher, IDoWork<GameObject> work, GridType gridType)
	{
		_phaseShift = searcher.PhaseShift;
		_doWork = work;
		GridType = gridType;
	}

	public void Visit(IList<GameObject> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var gameObject = objs[i];

			if (gameObject.InSamePhase(_phaseShift))
				_doWork.Invoke(gameObject);
		}
	}
}