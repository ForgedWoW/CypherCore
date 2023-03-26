// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Interfaces;
using Framework.Constants;

namespace Forged.MapServer.Maps.Grids;

public class GridCell
{
	/// <summary>
	///  Holds all World objects - Player, Pets, Corpse(resurrectable), DynamicObject(farsight)
	/// </summary>
    private readonly WorldObjectTypedList _objects;

	/// <summary>
	///  Holds all Grid objects - GameObjects, Creatures(except pets), DynamicObject, Corpse(Bones), AreaTrigger, Conversation, SceneObject
	/// </summary>
    private readonly WorldObjectTypedList _container;

	public GridCell()
	{
		_objects = new WorldObjectTypedList();
		_container = new WorldObjectTypedList();
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