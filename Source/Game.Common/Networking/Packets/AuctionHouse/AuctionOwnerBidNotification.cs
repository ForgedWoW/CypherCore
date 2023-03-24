// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.AuctionHouse;

public class AuctionOwnerBidNotification : ServerPacket
{
	public AuctionOwnerNotification Info;
	public ObjectGuid Bidder;
	public ulong MinIncrement;

	public AuctionOwnerBidNotification() : base(ServerOpcodes.AuctionOwnerBidNotification) { }

	public override void Write()
	{
		Info.Write(_worldPacket);
		_worldPacket.WriteUInt64(MinIncrement);
		_worldPacket.WritePackedGuid(Bidder);
	}
}
