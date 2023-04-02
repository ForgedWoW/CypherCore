// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.ClientReader;

public class WDCHeader
{
    public uint BitpackedDataOffset;
    public uint ColumnMetaSize;
    public uint CommonDataSize;
    public uint FieldCount;
    public HeaderFlags Flags;
    public int IdIndex;
    public uint LayoutHash;
    public int Locale;
    public uint LookupColumnCount;
    public int MaxId;
    public int MinId;
    public uint PalletDataSize;
    public uint RecordCount;
    public uint RecordSize;
    public uint SectionsCount;
    public uint Signature;
    public uint StringTableSize;

    public uint TableHash;
    public uint TotalFieldCount;
    public bool HasIndexTable()
    {
        return Convert.ToBoolean(Flags & HeaderFlags.IndexMap);
    }

    public bool HasOffsetTable()
    {
        return Convert.ToBoolean(Flags & HeaderFlags.OffsetMap);
    }
}