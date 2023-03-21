// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer;

public struct VisibleMapIdRef
{
	public VisibleMapIdRef(int references, TerrainSwapInfo visibleMapInfo)
	{
		References = references;
		VisibleMapInfo = visibleMapInfo;
	}

	public int References;
	public TerrainSwapInfo VisibleMapInfo;
}