// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.InteropServices;

namespace Forged.MapServer.DataStorage.ClientReader;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SparseEntry
{
	public int Offset;
	public ushort Size;
}