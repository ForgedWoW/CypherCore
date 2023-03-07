// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

class ObjectGridStoper : IGridNotifierCreature
{
	public ObjectGridStoper(GridType gridType)
	{
		GridType = gridType;
	}

	public GridType GridType { get; set; }

	public void Visit(IList<Creature> objs)
	{
		// stop any fights at grid de-activation and remove dynobjects/areatriggers created at cast by creatures
		for (var i = 0; i < objs.Count; ++i)
		{
			var creature = objs[i];
			creature.RemoveAllDynObjects();
			creature.RemoveAllAreaTriggers();

			if (creature.IsInCombat())
				creature.CombatStop();
		}
	}
}