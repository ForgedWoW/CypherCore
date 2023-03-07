// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class GameObjectWorker : IGridNotifierGameObject
{
	readonly PhaseShift _phaseShift;
	readonly IDoWork<GameObject> _doWork;

	public GameObjectWorker(WorldObject searcher, IDoWork<GameObject> work, GridType gridType)
	{
		_phaseShift = searcher.GetPhaseShift();
		_doWork = work;
		GridType = gridType;
	}

	public GridType GridType { get; set; }

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