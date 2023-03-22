// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.DataStorage;

public sealed class WorldMapOverlayRecord
{
	public uint Id;
	public uint UiMapArtID;
	public ushort TextureWidth;
	public ushort TextureHeight;
	public int OffsetX;
	public int OffsetY;
	public int HitRectTop;
	public int HitRectBottom;
	public int HitRectLeft;
	public int HitRectRight;
	public uint PlayerConditionID;
	public uint Flags;
	public uint[] AreaID = new uint[SharedConst.MaxWorldMapOverlayArea];
}