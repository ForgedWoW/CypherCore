// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps.Checks;

public class ObjectGUIDCheck : ICheck<WorldObject>
{
    private readonly ObjectGuid _gUid;

    public ObjectGUIDCheck(ObjectGuid guid)
    {
        _gUid = guid;
    }

    public bool Invoke(WorldObject obj)
    {
        return obj.GUID == _gUid;
    }

    public static implicit operator Predicate<WorldObject>(ObjectGUIDCheck check)
    {
        return check.Invoke;
    }
}