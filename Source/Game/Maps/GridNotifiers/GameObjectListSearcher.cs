// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class GameObjectListSearcher : IGridNotifierGameObject
{
	readonly PhaseShift _phaseShift;
	readonly List<GameObject> _objects;
	readonly ICheck<GameObject> _check;

	public GridType GridType { get; set; }

	public GameObjectListSearcher(WorldObject searcher, List<GameObject> objects, ICheck<GameObject> check, GridType gridType)
	{
		_phaseShift = searcher.GetPhaseShift();
		_objects = objects;
		_check = check;
		GridType = gridType;
	}

	public void Visit(IList<GameObject> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var gameObject = objs[i];

			if (gameObject.InSamePhase(_phaseShift))
				if (_check.Invoke(gameObject))
					_objects.Add(gameObject);
		}
	}
}