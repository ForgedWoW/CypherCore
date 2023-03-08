// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class CreatureWorker : IGridNotifierCreature
{
	readonly PhaseShift _phaseShift;
	readonly IDoWork<Creature> _doWork;

	public GridType GridType { get; set; }

	public CreatureWorker(WorldObject searcher, IDoWork<Creature> work, GridType gridType)
	{
		_phaseShift = searcher.GetPhaseShift();
		_doWork = work;
		GridType = gridType;
	}

	public void Visit(IList<Creature> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var creature = objs[i];

			if (creature.InSamePhase(_phaseShift))
				_doWork.Invoke(creature);
		}
	}
}