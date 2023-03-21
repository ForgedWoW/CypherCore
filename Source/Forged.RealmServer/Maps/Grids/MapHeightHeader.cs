// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Maps.Grids;

public struct MapHeightHeader
{
	public uint fourcc;
	public HeightHeaderFlags flags;
	public float gridHeight;
	public float gridMaxHeight;
}