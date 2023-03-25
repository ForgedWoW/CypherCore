﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

class BlackMarketBidOnItem : ClientPacket
{
	public ObjectGuid Guid;
	public uint MarketID;
	public ItemInstance Item = new();
	public ulong BidAmount;
	public BlackMarketBidOnItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
		MarketID = _worldPacket.ReadUInt32();
		BidAmount = _worldPacket.ReadUInt64();
		Item.Read(_worldPacket);
	}
}