﻿using System;
using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class UnitAuraCheck<T> : ICheck<T> where T : WorldObject
{
    public UnitAuraCheck(bool present, uint spellId, ObjectGuid casterGUID = default)
    {
        _present = present;
        _spellId = spellId;
        _casterGUID = casterGUID;
    }

    public bool Invoke(T obj)
    {
        return obj.ToUnit() && obj.ToUnit().HasAura(_spellId, _casterGUID) == _present;
    }

    public static implicit operator Predicate<T>(UnitAuraCheck<T> unit)
    {
        return unit.Invoke;
    }

    readonly bool _present;
    readonly uint _spellId;
    ObjectGuid _casterGUID;
}