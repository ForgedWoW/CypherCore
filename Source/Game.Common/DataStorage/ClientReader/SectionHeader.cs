// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.InteropServices;
using Game.DataStorage;

namespace Game.Common.DataStorage.ClientReader;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SectionHeader
{
	public ulong TactKeyLookup;
	public int FileOffset;
	public int NumRecords;
	public int StringTableSize;
	public int SparseTableOffset;    // CatalogDataOffset, absolute value, {uint offset, ushort size}[MaxId - MinId + 1]
	public int IndexDataSize;        // int indexData[IndexDataSize / 4]
	public int ParentLookupDataSize; // uint NumRecords, uint minId, uint maxId, {uint id, uint index}[NumRecords], questionable usefulness...
	public int NumSparseRecords;
	public int NumCopyRecords;
}
