// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

class AuctionOutbidNotification : ServerPacket
{
	public AuctionBidderNotification Info;
	public ulong BidAmount;
	public ulong MinIncrement;

	public AuctionOutbidNotification() : base(ServerOpcodes.AuctionOutbidNotification) { }

	public override void Write()
	{
		Info.Write(_worldPacket);
		_worldPacket.WriteUInt64(BidAmount);
		_worldPacket.WriteUInt64(MinIncrement);
	}
}