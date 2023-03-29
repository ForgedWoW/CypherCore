// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Dynamic;

public class FlagArray128 : FlagsArray<uint>
{
    public FlagArray128(uint p1 = 0, uint p2 = 0, uint p3 = 0, uint p4 = 0) : base(4)
    {
        _storage[0] = p1;
        _storage[1] = p2;
        _storage[2] = p3;
        _storage[3] = p4;
    }

    public FlagArray128(uint[] parts) : base(4)
    {
        _storage[0] = parts[0];
        _storage[1] = parts[1];
        _storage[2] = parts[2];
        _storage[3] = parts[3];
    }

    public bool IsEqual(params uint[] parts)
    {
        for (var i = 0; i < _storage.Length; ++i)
            if (_storage[i] == parts[i])
                return false;

        return true;
    }

    public bool HasFlag(params uint[] parts)
    {
        return (_storage[0] & parts[0] || _storage[1] & parts[1] || _storage[2] & parts[2] || _storage[3] & parts[3]);
    }

    public void Set(params uint[] parts)
    {
        for (var i = 0; i < parts.Length; ++i)
            _storage[i] = parts[i];
    }

    public static FlagArray128 operator &(FlagArray128 left, FlagArray128 right)
    {
        FlagArray128 fl = new();

        for (var i = 0; i < left._storage.Length; ++i)
            fl[i] = left._storage[i] & right._storage[i];

        return fl;
    }

    public static FlagArray128 operator |(FlagArray128 left, FlagArray128 right)
    {
        FlagArray128 fl = new();

        for (var i = 0; i < left._storage.Length; ++i)
            fl[i] = left._storage[i] | right._storage[i];

        return fl;
    }

    public static FlagArray128 operator ^(FlagArray128 left, FlagArray128 right)
    {
        FlagArray128 fl = new();

        for (var i = 0; i < left._storage.Length; ++i)
            fl[i] = left._storage[i] ^ right._storage[i];

        return fl;
    }
}