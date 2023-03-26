// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class GameObjectSearcher : IGridNotifierGameObject
{
    private readonly PhaseShift _phaseShift;
    private readonly ICheck<GameObject> _check;
    private GameObject _object;

	public GridType GridType { get; set; }

	public GameObjectSearcher(WorldObject searcher, ICheck<GameObject> check, GridType gridType)
	{
		_phaseShift = searcher.PhaseShift;
		_check = check;
		GridType = gridType;
	}

	public void Visit(IList<GameObject> objs)
	{
		// already found
		if (_object)
			return;

		for (var i = 0; i < objs.Count; ++i)
		{
			var gameObject = objs[i];

			if (!gameObject.InSamePhase(_phaseShift))
				continue;

			if (_check.Invoke(gameObject))
			{
				_object = gameObject;

				return;
			}
		}
	}

	public GameObject GetTarget()
	{
		return _object;
	}
}