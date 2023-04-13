// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Maps.Checks;

public class ObjectTypeIdCheck : ICheck<WorldObject>
{
    private readonly bool _equals;
    private readonly TypeId _typeId;
    public ObjectTypeIdCheck(TypeId typeId, bool equals)
    {
        _typeId = typeId;
        _equals = equals;
    }

    public bool Invoke(WorldObject obj)
    {
        return obj.TypeId == _typeId == _equals;
    }
}