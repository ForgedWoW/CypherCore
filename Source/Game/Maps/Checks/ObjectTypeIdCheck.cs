using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

public class ObjectTypeIdCheck : ICheck<WorldObject>
{
    public ObjectTypeIdCheck(TypeId typeId, bool equals)
    {
        _typeId = typeId;
        _equals = equals;
    }

    public bool Invoke(WorldObject obj)
    {
        return (obj.GetTypeId() == _typeId) == _equals;
    }

    readonly TypeId _typeId;
    readonly bool _equals;
}