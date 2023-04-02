// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Dynamic;

public class FlagsArray<T> where T : struct
{
    protected dynamic[] _storage;

    public T this[int i]
    {
        get { return _storage[i]; }
        set { _storage[i] = value; }
    }

    public FlagsArray(uint length)
    {
        _storage = new dynamic[length];
    }

    public FlagsArray(T[] parts)
    {
        _storage = new dynamic[parts.Length];

        for (var i = 0; i < parts.Length; ++i)
            _storage[i] = parts[i];
    }

    public FlagsArray(T[] parts, uint length)
    {
        for (var i = 0; i < parts.Length; ++i)
            _storage[i] = parts[i];
    }

    public static implicit operator bool(FlagsArray<T> left)
    {
        for (var i = 0; i < left.GetSize(); ++i)
            if ((dynamic)left[i] != 0)
                return true;

        return false;
    }

    public static FlagsArray<T> operator &(FlagsArray<T> left, FlagsArray<T> right)
    {
        FlagsArray<T> fl = new(left.GetSize());

        for (var i = 0; i < left.GetSize(); ++i)
            fl[i] = (dynamic)left[i] & right[i];

        return fl;
    }

    public static FlagsArray<T> operator ^(FlagsArray<T> left, FlagsArray<T> right)
    {
        FlagsArray<T> fl = new(left.GetSize());

        for (var i = 0; i < left.GetSize(); ++i)
            fl[i] = (dynamic)left[i] ^ right[i];

        return fl;
    }

    public static FlagsArray<T> operator |(FlagsArray<T> left, FlagsArray<T> right)
    {
        FlagsArray<T> fl = new(left.GetSize());

        for (var i = 0; i < left.GetSize(); ++i)
            fl[i] = (dynamic)left[i] | right[i];

        return fl;
    }

    public static bool operator <(FlagsArray<T> left, FlagsArray<T> right)
    {
        for (var i = (int)left.GetSize(); i > 0; --i)
            if ((dynamic)left[i - 1] < right[i - 1])
                return true;
            else if ((dynamic)left[i - 1] > right[i - 1])
                return false;

        return false;
    }

    public static bool operator >(FlagsArray<T> left, FlagsArray<T> right)
    {
        for (var i = (int)left.GetSize(); i > 0; --i)
            if ((dynamic)left[i - 1] > right[i - 1])
                return true;
            else if ((dynamic)left[i - 1] < right[i - 1])
                return false;

        return false;
    }

    public uint GetSize() => (uint)_storage.Length;
}