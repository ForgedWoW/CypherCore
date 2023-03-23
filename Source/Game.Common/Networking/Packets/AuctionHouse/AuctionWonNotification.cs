// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.AuctionHouse;

namespace Game.Common.Networking.Packets.AuctionHouse;

public class AuctionWonNotification : ServerPacket
{
	public AuctionBidderNotification Info;

	public AuctionWonNotification() : base(ServerOpcodes.AuctionWonNotification) { }

	public override void Write()
	{
		Info.Write(_worldPacket);
	}
}
