// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class UpdaterNotifier : IGridNotifierWorldObject
{
	readonly uint _timeDiff;

	public UpdaterNotifier(uint diff, GridType gridType)
	{
		_timeDiff = diff;
		GridType   = gridType;
	}

	public GridType GridType { get; set; }

	public void Visit(IList<WorldObject> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var obj = objs[i];

			if (obj == null || obj.IsTypeId(TypeId.Player) || obj.IsTypeId(TypeId.Corpse))
				continue;

			if (obj.IsInWorld)
				obj.Update(_timeDiff);
		}
	}
}