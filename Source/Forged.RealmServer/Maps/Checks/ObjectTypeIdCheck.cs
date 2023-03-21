// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Maps;

public class ObjectTypeIdCheck : ICheck<WorldObject>
{
	readonly TypeId _typeId;
	readonly bool _equals;

	public ObjectTypeIdCheck(TypeId typeId, bool equals)
	{
		_typeId = typeId;
		_equals = equals;
	}

	public bool Invoke(WorldObject obj)
	{
		return (obj.TypeId == _typeId) == _equals;
	}
}