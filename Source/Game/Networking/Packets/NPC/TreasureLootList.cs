﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Networking.Packets;

public class TreasureLootList
{
	public List<TreasureItem> Items = new();

	public void Write(WorldPacket data)
	{
		data.WriteInt32(Items.Count);

		foreach (var treasureItem in Items)
			treasureItem.Write(data);
	}
}