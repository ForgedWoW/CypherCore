// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.AuctionHouse;

public class AuctionSetFavoriteItem : ClientPacket
{
	public AuctionFavoriteInfo Item;
	public bool IsNotFavorite = true;

	public AuctionSetFavoriteItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		IsNotFavorite = _worldPacket.HasBit();
		Item = new AuctionFavoriteInfo(_worldPacket);
	}
}
