// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class VisibleChangesNotifier : IGridNotifierCreature, IGridNotifierPlayer, IGridNotifierDynamicObject
{
	readonly ICollection<WorldObject> _objects;

	public GridType GridType { get; set; }

	public VisibleChangesNotifier(ICollection<WorldObject> objects, GridType gridType)
	{
		_objects = objects;
		GridType = gridType;
	}

	public void Visit(IList<Creature> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var creature = objs[i];

			if (creature == null) continue;

			foreach (var visionPlayer in creature.GetSharedVisionList())
				if (visionPlayer.SeerView == creature)
					visionPlayer.UpdateVisibilityOf(_objects);
		}
	}

	public void Visit(IList<DynamicObject> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var dynamicObject = objs[i];
			var caster = dynamicObject.GetCaster();

			if (caster)
			{
				var pl = caster.AsPlayer;

				if (pl && pl.SeerView == dynamicObject)
					pl.UpdateVisibilityOf(_objects);
			}
		}
	}

	public void Visit(IList<Player> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var player = objs[i];

			if (player == null) continue;

			player.UpdateVisibilityOf(_objects);

			foreach (var visionPlayer in player.GetSharedVisionList())
				if (visionPlayer.SeerView == player)
					visionPlayer.UpdateVisibilityOf(_objects);
		}
	}
}

//Searchers

//Checks

#region Checks

// Success at unit in range, range update for next check (this can be use with UnitLastSearcher to find nearest unit)

// Success at unit in range, range update for next check (this can be use with CreatureLastSearcher to find nearest creature)

// Find the nearest Fishing hole and return true only if source object is in range of hole

// Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest GO)

// Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest unspawned GO)

// Success at unit in range, range update for next check (this can be use with GameobjectLastSearcher to find nearest GO with a certain type)

// CHECK modifiers

#endregion