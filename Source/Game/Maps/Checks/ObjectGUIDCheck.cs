using System;
using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class ObjectGUIDCheck : ICheck<WorldObject>
{
    public ObjectGUIDCheck(ObjectGuid GUID)
    {
        _GUID = GUID;
    }

    public bool Invoke(WorldObject obj)
    {
        return obj.GetGUID() == _GUID;
    }

    public static implicit operator Predicate<WorldObject>(ObjectGUIDCheck check)
    {
        return check.Invoke;
    }

    ObjectGuid _GUID;
}