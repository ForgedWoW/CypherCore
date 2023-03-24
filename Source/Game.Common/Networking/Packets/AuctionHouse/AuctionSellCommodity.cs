// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Common.Entities.Objects;
using Game.Common.Networking.Packets.Addon;

namespace Game.Common.Networking.Packets.AuctionHouse;

public class AuctionSellCommodity : ClientPacket
{
	public ObjectGuid Auctioneer;
	public ulong UnitPrice;
	public uint RunTime;
	public AddOnInfo? TaintedBy;
	public Array<AuctionItemForSale> Items = new(64);

	public AuctionSellCommodity(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Auctioneer = _worldPacket.ReadPackedGuid();
		UnitPrice = _worldPacket.ReadUInt64();
		RunTime = _worldPacket.ReadUInt32();

		if (_worldPacket.HasBit())
			TaintedBy = new AddOnInfo();

		var itemCount = _worldPacket.ReadBits<uint>(6);

		if (TaintedBy.HasValue)
			TaintedBy.Value.Read(_worldPacket);

		for (var i = 0; i < itemCount; ++i)
			Items[i] = new AuctionItemForSale(_worldPacket);
	}
}
