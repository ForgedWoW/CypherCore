// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Maps.Grids;

public struct MapLiquidHeader
{
    public uint fourcc;
    public LiquidHeaderFlags flags;
    public byte liquidFlags;
    public ushort liquidType;
    public byte offsetX;
    public byte offsetY;
    public byte width;
    public byte height;
    public float liquidLevel;
}