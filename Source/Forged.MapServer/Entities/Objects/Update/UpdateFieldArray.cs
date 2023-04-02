﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections;
using System.Collections.Generic;

namespace Forged.MapServer.Entities.Objects.Update;

public class UpdateFieldArray<T> : IEnumerable<T> where T : new()
{
    public UpdateFieldArray(uint size, int bit, int firstElementBit)
    {
        Values = new T[size];

        for (var i = 0; i < size; ++i)
            Values[i] = new T();

        Bit = bit;
        FirstElementBit = firstElementBit;
    }

    public int Bit { get; set; }
    public int FirstElementBit { get; set; }
    public T[] Values { get; set; }
    public T this[int index]
    {
        get => Values[index];
        set => Values[index] = value;
    }
    public IEnumerator<T> GetEnumerator()
    {
        foreach (var obj in Values)
            yield return obj;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int GetSize()
    {
        return Values.Length;
    }
}