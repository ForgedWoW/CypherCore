﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Maps.Interfaces;

namespace Forged.RealmServer.Maps;

class ObjectGridCleaner : IGridNotifierWorldObject
{
	public GridType GridType { get; set; }

	public ObjectGridCleaner(GridType gridType)
	{
		GridType = gridType;
	}

	public void Visit(IList<WorldObject> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var obj = objs[i];

			if (obj.IsTypeId(TypeId.Player))
				continue;

			obj.SetDestroyedObject(true);
			obj.CleanupsBeforeDelete();
		}
	}
}