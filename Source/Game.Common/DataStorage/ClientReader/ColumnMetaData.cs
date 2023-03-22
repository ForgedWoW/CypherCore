// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.InteropServices;
using Framework.Constants;

namespace Game.DataStorage;

[StructLayout(LayoutKind.Explicit)]
public struct ColumnMetaData
{
	[FieldOffset(0)] public ushort RecordOffset;
	[FieldOffset(2)] public ushort Size;
	[FieldOffset(4)] public uint AdditionalDataSize;
	[FieldOffset(8)] public DB2ColumnCompression CompressionType;
	[FieldOffset(12)] public ColumnCompressionData_Immediate Immediate;
	[FieldOffset(12)] public ColumnCompressionData_Pallet Pallet;
	[FieldOffset(12)] public ColumnCompressionData_Common Common;
}