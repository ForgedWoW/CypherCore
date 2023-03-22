﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

struct SpellReducedReagent
{
	public int ItemID;
	public int Quantity;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(ItemID);
		data.WriteInt32(Quantity);
	}
}