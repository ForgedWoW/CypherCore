// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

class AuctionWonNotification : ServerPacket
{
	public AuctionBidderNotification Info;

	public AuctionWonNotification() : base(ServerOpcodes.AuctionWonNotification) { }

	public override void Write()
	{
		Info.Write(_worldPacket);
	}
}