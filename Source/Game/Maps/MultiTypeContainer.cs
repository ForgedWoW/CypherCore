// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class MultiTypeContainer
{
	private readonly List<Player> _players = new();
	private readonly List<Creature> _creatures = new();
	private readonly List<Corpse> _corpses = new();
	private readonly List<DynamicObject> _dynamicObjects = new();
	private readonly List<AreaTrigger> _areaTriggers = new();
	private readonly List<SceneObject> _sceneObjects = new();
	private readonly List<Conversation> _conversations = new();
	private readonly List<GameObject> _gameObjects = new();
	private readonly List<WorldObject> _worldObjects = new();

	public void Insert(WorldObject obj)
	{
		if (obj == null)
		{
			Log.outWarn(LogFilter.Maps, $"Tried to insert null during {nameof(WorldObject)} to {nameof(MultiTypeContainer)}");
			Log.outWarn(LogFilter.Maps, Environment.StackTrace);

			return;
		}

		lock (_worldObjects)
		{
			_worldObjects.Add(obj);

			switch (obj.GetTypeId())
			{
				case TypeId.Unit:
					_creatures.Add((Creature)obj);

					break;
				case TypeId.Player:
					_players.Add((Player)obj);

					break;
				case TypeId.GameObject:
					_gameObjects.Add((GameObject)obj);

					break;
				case TypeId.DynamicObject:
					_dynamicObjects.Add((DynamicObject)obj);

					break;
				case TypeId.Corpse:
					_corpses.Add((Corpse)obj);

					break;
				case TypeId.AreaTrigger:
					_areaTriggers.Add((AreaTrigger)obj);

					break;
				case TypeId.SceneObject:
					_sceneObjects.Add((SceneObject)obj);

					break;
				case TypeId.Conversation:
					_conversations.Add((Conversation)obj);

					break;
			}
		}
	}

	public void Remove(WorldObject obj)
	{
		if (obj == null)
		{
			Log.outWarn(LogFilter.Maps, $"Tried to remove null during {nameof(WorldObject)} to {nameof(MultiTypeContainer)}");
			Log.outWarn(LogFilter.Maps, Environment.StackTrace);

			return;
		}

		lock (_worldObjects)
		{
			_worldObjects.Remove(obj);

			switch (obj.GetTypeId())
			{
				case TypeId.Unit:
					_creatures.Remove((Creature)obj);

					break;
				case TypeId.Player:
					_players.Remove((Player)obj);

					break;
				case TypeId.GameObject:
					_gameObjects.Remove((GameObject)obj);

					break;
				case TypeId.DynamicObject:
					_dynamicObjects.Remove((DynamicObject)obj);

					break;
				case TypeId.Corpse:
					_corpses.Remove((Corpse)obj);

					break;
				case TypeId.AreaTrigger:
					_areaTriggers.Remove((AreaTrigger)obj);

					break;
				case TypeId.SceneObject:
					_sceneObjects.Remove((SceneObject)obj);

					break;
				case TypeId.Conversation:
					_conversations.Remove((Conversation)obj);

					break;
			}
		}
	}

	public void Visit(IGridNotifier visitor)
	{
		if (visitor is IGridNotifierGameObject go)
			go.Visit(_gameObjects);

		if (visitor is IGridNotifierCreature cr)
			cr.Visit(_creatures);

		if (visitor is IGridNotifierDynamicObject dyn)
			dyn.Visit(_dynamicObjects);

		if (visitor is IGridNotifierCorpse cor)
			cor.Visit(_corpses);

		if (visitor is IGridNotifierAreaTrigger at)
			at.Visit(_areaTriggers);

		if (visitor is IGridNotifierSceneObject so)
			so.Visit(_sceneObjects);

		if (visitor is IGridNotifierConversation conv)
			conv.Visit(_conversations);

		if (visitor is IGridNotifierWorldObject wo)
			wo.Visit(_worldObjects);

		if (visitor is IGridNotifierPlayer p)
			p.Visit(_players);
	}

	public bool Contains(WorldObject obj)
	{
		lock (_worldObjects)
		{
			return _worldObjects.Contains(obj);
		}
	}

	public int GetCount<T>()
	{
		lock (_worldObjects)
		{
			switch (typeof(T).Name)
			{
				case nameof(Creature):
					return _creatures.Count;
				case nameof(Player):
					return _players.Count;
				case nameof(GameObject):
					return _gameObjects.Count;
				case nameof(DynamicObject):
					return _dynamicObjects.Count;
				case nameof(Corpse):
					return _corpses.Count;
				case nameof(AreaTrigger):
					return _areaTriggers.Count;
				case nameof(Conversation):
					return _conversations.Count;
				case nameof(SceneObject):
					return _sceneObjects.Count;
			}
		}

		return 0;
	}
}