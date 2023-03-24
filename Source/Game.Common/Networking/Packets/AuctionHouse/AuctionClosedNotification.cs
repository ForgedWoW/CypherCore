// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.AuctionHouse;

public class AuctionClosedNotification : ServerPacket
{
	public AuctionOwnerNotification Info;
	public float ProceedsMailDelay;
	public bool Sold = true;

	public AuctionClosedNotification() : base(ServerOpcodes.AuctionClosedNotification) { }

	public override void Write()
	{
		Info.Write(_worldPacket);
		_worldPacket.WriteFloat(ProceedsMailDelay);
		_worldPacket.WriteBit(Sold);
		_worldPacket.FlushBits();
	}
}
