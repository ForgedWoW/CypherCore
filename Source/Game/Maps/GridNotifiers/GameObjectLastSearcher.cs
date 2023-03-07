﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class GameObjectLastSearcher : IGridNotifierGameObject
{
	readonly PhaseShift _phaseShift;
	readonly ICheck<GameObject> _check;
	GameObject _object;

	public GameObjectLastSearcher(WorldObject searcher, ICheck<GameObject> check, GridType gridType)
	{
		_phaseShift = searcher.GetPhaseShift();
		_check      = check;
		GridType     = gridType;
	}

	public GridType GridType { get; set; }

	public void Visit(IList<GameObject> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var gameObject = objs[i];

			if (!gameObject.InSamePhase(_phaseShift))
				continue;

			if (_check.Invoke(gameObject))
				_object = gameObject;
		}
	}

	public GameObject GetTarget()
	{
		return _object;
	}
}