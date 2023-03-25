// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class AuctionConfirmCommoditiesPurchase : ClientPacket
{
	public ObjectGuid Auctioneer;
	public int ItemID;
	public uint Quantity;
	public AddOnInfo? TaintedBy;

	public AuctionConfirmCommoditiesPurchase(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Auctioneer = _worldPacket.ReadPackedGuid();
		ItemID = _worldPacket.ReadInt32();
		Quantity = _worldPacket.ReadUInt32();

		if (_worldPacket.HasBit())
		{
			TaintedBy = new AddOnInfo();
			TaintedBy.Value.Read(_worldPacket);
		}
	}
}