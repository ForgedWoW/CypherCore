// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Maps.Interfaces;

namespace Forged.RealmServer.Maps;

internal class ObjectGridUnloader : IGridNotifierWorldObject
{
	public GridType GridType { get; set; }

	internal ObjectGridUnloader(GridType gridType = GridType.Grid)
	{
		GridType = gridType;
	}

	public void Visit(IList<WorldObject> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var obj = objs[i];

			if (obj.IsTypeId(TypeId.Corpse))
				continue;

			//Some creatures may summon other temp summons in CleanupsBeforeDelete()
			//So we need this even after cleaner (maybe we can remove cleaner)
			//Example: Flame Leviathan Turret 33139 is summoned when a creature is deleted
			//TODO: Check if that script has the correct logic. Do we really need to summons something before deleting?
			obj.CleanupsBeforeDelete();
			obj.Dispose();
		}
	}
}