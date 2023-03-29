﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Objects.Update;

public class DynamicUpdateFieldSetter<T> : IUpdateField<T> where T : new()
{
    private readonly DynamicUpdateField<T> _dynamicUpdateField;
    private readonly int _index;

    public DynamicUpdateFieldSetter(DynamicUpdateField<T> dynamicUpdateField, int index)
    {
        _dynamicUpdateField = dynamicUpdateField;
        _index = index;
    }

    public void SetValue(T value)
    {
        _dynamicUpdateField[_index] = value;
    }

    public T GetValue()
    {
        return _dynamicUpdateField[_index];
    }

    public static implicit operator T(DynamicUpdateFieldSetter<T> dynamicUpdateFieldSetter)
    {
        return dynamicUpdateFieldSetter.GetValue();
    }
}