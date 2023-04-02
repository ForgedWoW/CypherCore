// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Maps.Grids;

public struct MapFileHeader
{
    public uint areaMapOffset;
    public uint areaMapSize;
    public uint buildMagic;
    public uint heightMapOffset;
    public uint heightMapSize;
    public uint holesOffset;
    public uint holesSize;
    public uint liquidMapOffset;
    public uint liquidMapSize;
    public uint mapMagic;
    public uint versionMagic;
}