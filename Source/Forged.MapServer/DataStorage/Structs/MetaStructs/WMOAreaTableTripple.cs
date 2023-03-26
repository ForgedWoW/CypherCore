// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.MetaStructs;

public struct WMOAreaTableTripple
{
	public WMOAreaTableTripple(int r, int a, int g)
	{
		groupId = g;
		rootId = r;
		adtId = a;
	}

	// ordered by entropy; that way memcmp will have a minimal medium runtime
    private readonly int groupId;
    private readonly int rootId;
    private readonly int adtId;
}