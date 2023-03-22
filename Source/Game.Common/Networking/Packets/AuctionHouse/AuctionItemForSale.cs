﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public struct AuctionItemForSale
{
	public ObjectGuid Guid;
	public uint UseCount;

	public AuctionItemForSale(WorldPacket data)
	{
		Guid = data.ReadPackedGuid();
		UseCount = data.ReadUInt32();
	}
}