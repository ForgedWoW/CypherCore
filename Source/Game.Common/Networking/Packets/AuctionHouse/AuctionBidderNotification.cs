// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Items;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
using Game.Common.Networking.Packets.Item;

namespace Game.Common.Networking.Packets.AuctionHouse;

struct AuctionBidderNotification
{
	public uint AuctionID;
	public ObjectGuid Bidder;
	public ItemInstance Item;

	public void Initialize(AuctionPosting auction, Item item)
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
