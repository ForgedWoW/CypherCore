// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class AuctionHelloResponse : ServerPacket
{
	public ObjectGuid Guid;
	public uint DeliveryDelay;
	public bool OpenForBusiness = true;

	public AuctionHelloResponse() : base(ServerOpcodes.AuctionHelloResponse) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteUInt32(DeliveryDelay);
		_worldPacket.WriteBit(OpenForBusiness);
		_worldPacket.FlushBits();
	}
}