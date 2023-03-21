// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Maps.Interfaces;

namespace Forged.RealmServer.Maps;

class ObjectGridStoper : IGridNotifierCreature
{
	public GridType GridType { get; set; }

	public ObjectGridStoper(GridType gridType)
	{
		GridType = gridType;
	}

	public void Visit(IList<Creature> objs)
	{
		// stop any fights at grid de-activation and remove dynobjects/areatriggers created at cast by creatures
		for (var i = 0; i < objs.Count; ++i)
		{
			var creature = objs[i];
			creature.RemoveAllDynObjects();
			creature.RemoveAllAreaTriggers();

			if (creature.IsInCombat)
				creature.CombatStop();
		}
	}
}