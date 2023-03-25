// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Maps.Grids;

public struct MapAreaHeader
{
	public uint fourcc;
	public AreaHeaderFlags flags;
	public ushort gridArea;
}