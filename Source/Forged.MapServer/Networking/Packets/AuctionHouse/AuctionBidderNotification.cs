// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AuctionHouse;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal struct AuctionBidderNotification
{
	public uint AuctionID;
	public ObjectGuid Bidder;
	public ItemInstance Item;

	public void Initialize(AuctionPosting auction, Entities.Items.Item item)
	{
		AuctionID = auction.Id;
		Item = new ItemInstance(item);
		Bidder = auction.Bidder;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(AuctionID);
		data.WritePackedGuid(Bidder);
		Item.Write(data);
	}
}