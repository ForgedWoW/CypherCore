// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class WorldObjectListSearcher : IGridNotifierPlayer, IGridNotifierCreature, IGridNotifierCorpse, IGridNotifierGameObject, IGridNotifierDynamicObject, IGridNotifierAreaTrigger, IGridNotifierSceneObject, IGridNotifierConversation
{
	readonly List<WorldObject> _objects;
	readonly PhaseShift _phaseShift;
	readonly ICheck<WorldObject> _check;

	public GridType GridType { get; set; }

	public GridMapTypeMask Mask { get; set; }

	public WorldObjectListSearcher(WorldObject searcher, List<WorldObject> objects, ICheck<WorldObject> check, GridMapTypeMask mapTypeMask = GridMapTypeMask.All, GridType gridType = GridType.All)
	{
		Mask = mapTypeMask;
		_phaseShift = searcher.PhaseShift;
		_objects = objects;
		_check = check;
		GridType = gridType;
	}

	public void Visit(IList<AreaTrigger> objs)
	{
		if (!Mask.HasAnyFlag(GridMapTypeMask.AreaTrigger))
			return;

		for (var i = 0; i < objs.Count; ++i)
		{
			var areaTrigger = objs[i];

			if (areaTrigger.InSamePhase(_phaseShift) && _check.Invoke(areaTrigger))
				_objects.Add(areaTrigger);
		}
	}

	public void Visit(IList<Conversation> objs)
	{
		if (!Mask.HasAnyFlag(GridMapTypeMask.Conversation))
			return;

		for (var i = 0; i < objs.Count; ++i)
		{
			var conversation = objs[i];

			if (conversation.InSamePhase(_phaseShift) && _check.Invoke(conversation))
				_objects.Add(conversation);
		}
	}

	public void Visit(IList<Corpse> objs)
	{
		if (!Mask.HasAnyFlag(GridMapTypeMask.Corpse))
			return;

		for (var i = 0; i < objs.Count; ++i)
		{
			var corpse = objs[i];

			if (corpse.InSamePhase(_phaseShift) && _check.Invoke(corpse))
				_objects.Add(corpse);
		}
	}

	public void Visit(IList<Creature> objs)
	{
		if (!Mask.HasAnyFlag(GridMapTypeMask.Creature))
			return;

		for (var i = 0; i < objs.Count; ++i)
		{
			var creature = objs[i];

			if (creature.InSamePhase(_phaseShift) && _check.Invoke(creature))
				_objects.Add(creature);
		}
	}

	public void Visit(IList<DynamicObject> objs)
	{
		if (!Mask.HasAnyFlag(GridMapTypeMask.DynamicObject))
			return;

		for (var i = 0; i < objs.Count; ++i)
		{
			var dynamicObject = objs[i];

			if (dynamicObject.InSamePhase(_phaseShift) && _check.Invoke(dynamicObject))
				_objects.Add(dynamicObject);
		}
	}

	public void Visit(IList<GameObject> objs)
	{
		if (!Mask.HasAnyFlag(GridMapTypeMask.GameObject))
			return;

		for (var i = 0; i < objs.Count; ++i)
		{
			var gameObject = objs[i];

			if (gameObject.InSamePhase(_phaseShift) && _check.Invoke(gameObject))
				_objects.Add(gameObject);
		}
	}

	public void Visit(IList<Player> objs)
	{
		if (!Mask.HasAnyFlag(GridMapTypeMask.Player))
			return;

		for (var i = 0; i < objs.Count; ++i)
		{
			var player = objs[i];

			if (player.InSamePhase(_phaseShift) && _check.Invoke(player))
				_objects.Add(player);
		}
	}

	public void Visit(IList<SceneObject> objs)
	{
		if (!Mask.HasAnyFlag(GridMapTypeMask.Conversation))
			return;

		for (var i = 0; i < objs.Count; ++i)
		{
			var sceneObject = objs[i];

			if (_check.Invoke(sceneObject))
				_objects.Add(sceneObject);
		}
	}
}