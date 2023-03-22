// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public struct AuctionOwnerNotification
{
	public uint AuctionID;
	public ulong BidAmount;
	public ItemInstance Item;

	public void Initialize(AuctionPosting auction)
	{
		AuctionID = auction.Id;
		Item = new ItemInstance(auction.Items[0]);
		BidAmount = auction.BidAmount;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(AuctionID);
		data.WriteUInt64(BidAmount);
		Item.Write(data);
	}
}