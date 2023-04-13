// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Maps.Grids;

public struct MapFileHeader
{
    public uint AreaMapOffset;
    public uint AreaMapSize;
    public uint BuildMagic;
    public uint HeightMapOffset;
    public uint HeightMapSize;
    public uint HolesOffset;
    public uint HolesSize;
    public uint LiquidMapOffset;
    public uint LiquidMapSize;
    public uint MapMagic;
    public uint VersionMagic;
}