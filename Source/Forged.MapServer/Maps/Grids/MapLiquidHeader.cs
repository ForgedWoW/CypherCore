// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Maps.Grids;

public struct MapLiquidHeader
{
    public LiquidHeaderFlags Flags;
    public uint Fourcc;
    public byte Height;
    public byte LiquidFlags;
    public float LiquidLevel;
    public ushort LiquidType;
    public byte OffsetX;
    public byte OffsetY;
    public byte Width;
}