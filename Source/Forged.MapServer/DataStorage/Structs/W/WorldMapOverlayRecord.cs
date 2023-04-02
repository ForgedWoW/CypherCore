// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.W;

public sealed class WorldMapOverlayRecord
{
    public uint[] AreaID = new uint[SharedConst.MaxWorldMapOverlayArea];
    public uint Flags;
    public int HitRectBottom;
    public int HitRectLeft;
    public int HitRectRight;
    public int HitRectTop;
    public uint Id;
    public int OffsetX;
    public int OffsetY;
    public uint PlayerConditionID;
    public ushort TextureHeight;
    public ushort TextureWidth;
    public uint UiMapArtID;
}