// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.AI.SmartScripts;

class ObjectGuidList
{
	readonly List<ObjectGuid> _guidList = new();
	readonly List<WorldObject> _objectList = new();

	public ObjectGuidList(List<WorldObject> objectList)
	{
		_objectList = objectList;

		foreach (var obj in _objectList)
			_guidList.Add(obj.GUID);
	}

	public List<WorldObject> GetObjectList(WorldObject obj)
	{
		UpdateObjects(obj);

		return _objectList;
	}

	public void AddGuid(ObjectGuid guid)
	{
		_guidList.Add(guid);
	}

	//sanitize vector using _guidVector
	void UpdateObjects(WorldObject obj)
	{
		_objectList.Clear();

		foreach (var guid in _guidList)
		{
			var newObj = Global.ObjAccessor.GetWorldObject(obj, guid);

			if (newObj != null)
				_objectList.Add(newObj);
		}
	}
}