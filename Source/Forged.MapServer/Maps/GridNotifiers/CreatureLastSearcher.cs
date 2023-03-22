﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class CreatureLastSearcher : IGridNotifierCreature
{
	internal PhaseShift _phaseShift;
	readonly ICheck<Creature> _check;
	Creature _object;

	public GridType GridType { get; set; }

	public CreatureLastSearcher(WorldObject searcher, ICheck<Creature> check, GridType gridType)
	{
		_phaseShift = searcher.PhaseShift;
		_check = check;
		GridType = gridType;
	}

	public void Visit(IList<Creature> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var creature = objs[i];

			if (!creature.InSamePhase(_phaseShift))
				continue;

			if (_check.Invoke(creature))
				_object = creature;
		}
	}

	public Creature GetTarget()
	{
		return _object;
	}
}