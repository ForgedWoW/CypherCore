// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Runtime.InteropServices;

namespace Forged.MapServer.DataStorage.ClientReader;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FieldMetaData
{
    public short Bits;
    public ushort Offset;

    public int ByteCount
    {
        get
        {
            var value = (32 - Bits) >> 3;

            return value < 0 ? Math.Abs(value) + 4 : value;
        }
    }

    public int BitCount
    {
        get
        {
            var bitSize = 32 - Bits;

            if (bitSize < 0)
                bitSize = bitSize * -1 + 32;

            return bitSize;
        }
    }

    public FieldMetaData(short bits, ushort offset)
    {
        Bits = bits;
        Offset = offset;
    }
}