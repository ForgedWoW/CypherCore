// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Threading;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class UpdaterNotifier : IGridNotifierWorldObject
{
	readonly uint _timeDiff;
	readonly LimitedThreadTaskManager _threadManager;
	readonly HashSet<WorldObject> _worldObjects = new();

	public GridType GridType { get; set; }

	public UpdaterNotifier(uint diff, GridType gridType, LimitedThreadTaskManager taskManager)
	{
		_timeDiff = diff;
		GridType = gridType;
		_threadManager = taskManager;
	}

	public void Visit(IList<WorldObject> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var obj = objs[i];

			if (obj == null || obj.IsTypeId(TypeId.Player) || obj.IsTypeId(TypeId.Corpse))
				continue;

			if (obj.IsInWorld)
				_worldObjects.Add(obj);
		}
	}

	public void ExecuteUpdate()
	{
		foreach (var obj in _worldObjects)
			_threadManager.Schedule(() => obj.Update(_timeDiff));
	}
}