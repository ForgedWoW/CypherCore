﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

struct ItemPurchaseRefundItem
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt32(ItemID);
		data.WriteUInt32(ItemCount);
	}

	public uint ItemID;
	public uint ItemCount;
}