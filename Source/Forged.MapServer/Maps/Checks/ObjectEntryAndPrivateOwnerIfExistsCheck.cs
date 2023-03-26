// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps.Checks;

internal class ObjectEntryAndPrivateOwnerIfExistsCheck : ICheck<WorldObject>
{
    private readonly uint _entry;
    private readonly ObjectGuid _ownerGUID;

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