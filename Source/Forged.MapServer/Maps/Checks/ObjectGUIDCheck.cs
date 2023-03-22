// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class ObjectGUIDCheck : ICheck<WorldObject>
{
	readonly ObjectGuid _gUID;

	public ObjectGUIDCheck(ObjectGuid GUID)
	{
		_gUID = GUID;
	}

	public bool Invoke(WorldObject obj)
	{
		return obj.GUID == _gUID;
	}

	public static implicit operator Predicate<WorldObject>(ObjectGUIDCheck check)
	{
		return check.Invoke;
	}
}