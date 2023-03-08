// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class ObjectEntryAndPrivateOwnerIfExistsCheck : ICheck<WorldObject>
{
	readonly uint _entry;
	readonly ObjectGuid _ownerGUID;

	public ObjectEntryAndPrivateOwnerIfExistsCheck(ObjectGuid ownerGUID, uint entry)
	{
		_ownerGUID = ownerGUID;
		_entry = entry;
	}

	public bool Invoke(WorldObject obj)
	{
		return obj.Entry == _entry && (!obj.IsPrivateObject || obj.PrivateObjectOwner == _ownerGUID);
	}
}