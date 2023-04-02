﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps.Checks;

public class UnitAuraCheck<T> : ICheck<T> where T : WorldObject
{
    private readonly ObjectGuid _casterGUID;
    private readonly bool _present;
    private readonly uint _spellId;
    public UnitAuraCheck(bool present, uint spellId, ObjectGuid casterGUID = default)
    {
        _present = present;
        _spellId = spellId;
        _casterGUID = casterGUID;
    }

    public static implicit operator Predicate<T>(UnitAuraCheck<T> unit)
    {
        return unit.Invoke;
    }

    public bool Invoke(T obj)
    {
        return obj.AsUnit && obj.AsUnit.HasAura(_spellId, _casterGUID) == _present;
    }
}