// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game;

public struct MmapTileHeader
{
	public uint mmapMagic;
	public uint dtVersion;
	public uint mmapVersion;
	public uint size;
	public byte usesLiquids;
}