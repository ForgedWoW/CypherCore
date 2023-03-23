﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;
using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Entities.Objects.Update;

public class MultiFloorExplore
{
	public List<int> WorldMapOverlayIDs = new();

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteInt32(WorldMapOverlayIDs.Count);

		for (var i = 0; i < WorldMapOverlayIDs.Count; ++i)
			data.WriteInt32(WorldMapOverlayIDs[i]);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		data.WriteInt32(WorldMapOverlayIDs.Count);

		for (var i = 0; i < WorldMapOverlayIDs.Count; ++i)
			data.WriteInt32(WorldMapOverlayIDs[i]);

		data.FlushBits();
	}
}
