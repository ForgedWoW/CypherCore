// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Maps.Grids;

public struct MapFileHeader
{
	public uint mapMagic;
	public uint versionMagic;
	public uint buildMagic;
	public uint areaMapOffset;
	public uint areaMapSize;
	public uint heightMapOffset;
	public uint heightMapSize;
	public uint liquidMapOffset;
	public uint liquidMapSize;
	public uint holesOffset;
	public uint holesSize;
}