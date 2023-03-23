// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.DataStorage;

namespace Game.Common.DataStorage.ClientReader;

public class WDCHeader
{
	public uint Signature;
	public uint RecordCount;
	public uint FieldCount;
	public uint RecordSize;
	public uint StringTableSize;

	public uint TableHash;
	public uint LayoutHash;
	public int MinId;
	public int MaxId;
	public int Locale;
	public HeaderFlags Flags;
	public int IdIndex;
	public uint TotalFieldCount;
	public uint BitpackedDataOffset;
	public uint LookupColumnCount;
	public uint ColumnMetaSize;
	public uint CommonDataSize;
	public uint PalletDataSize;
	public uint SectionsCount;

	public bool HasIndexTable()
	{
		return Convert.ToBoolean(Flags & HeaderFlags.IndexMap);
	}

	public bool HasOffsetTable()
	{
		return Convert.ToBoolean(Flags & HeaderFlags.OffsetMap);
	}
}
