// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Maps.Interfaces;

namespace Forged.RealmServer.Maps;

public class CreatureListSearcher : IGridNotifierCreature
{
	internal PhaseShift _phaseShift;
	readonly List<Creature> _objects;
	readonly ICheck<Creature> _check;

	public GridType GridType { get; set; }


	public CreatureListSearcher(WorldObject searcher, List<Creature> objects, ICheck<Creature> check, GridType gridType)
	{
		_phaseShift = searcher.PhaseShift;
		_objects = objects;
		_check = check;
		GridType = gridType;
	}

	public void Visit(IList<Creature> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var creature = objs[i];

			if (creature.InSamePhase(_phaseShift))
				if (_check.Invoke(creature))
					_objects.Add(creature);
		}
	}
}