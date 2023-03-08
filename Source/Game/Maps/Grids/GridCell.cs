// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps.Grids;

public class GridCell
{
	/// <summary>
	///  Holds all World objects - Player, Pets, Corpse(resurrectable), DynamicObject(farsight)
	/// </summary>
	readonly MultiTypeContainer _objects;

	/// <summary>
	///  Holds all Grid objects - GameObjects, Creatures(except pets), DynamicObject, Corpse(Bones), AreaTrigger, Conversation, SceneObject
	/// </summary>
	readonly MultiTypeContainer _container;

	public GridCell()
	{
		_objects = new MultiTypeContainer();
		_container = new MultiTypeContainer();
	}

	public void Visit(IGridNotifier visitor)
	{
		if (visitor.GridType.HasFlag(GridType.Grid))
			_container.Visit(visitor);

		if (visitor.GridType.HasFlag(GridType.World))
			_objects.Visit(visitor);
	}

	public uint GetWorldObjectCountInGrid<T>() where T : WorldObject
	{
		return (uint)_objects.GetCount<T>();
	}

	public void AddWorldObject(WorldObject obj)
	{
		_objects.Insert(obj);
	}

	public void AddGridObject(WorldObject obj)
	{
		_container.Insert(obj);
	}

	public void RemoveWorldObject(WorldObject obj)
	{
		_objects.Remove(obj);
	}

	public void RemoveGridObject(WorldObject obj)
	{
		_container.Remove(obj);
	}
}