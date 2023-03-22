// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class AuctionGetCommodityQuoteResult : ServerPacket
{
	public ulong? TotalPrice;
	public uint? Quantity;
	public int? QuoteDuration;
	public int ItemID;
	public uint DesiredDelay;

	public AuctionGetCommodityQuoteResult() : base(ServerOpcodes.AuctionGetCommodityQuoteResult) { }

	public override void Write()
	{
		_worldPacket.WriteBit(TotalPrice.HasValue);
		_worldPacket.WriteBit(Quantity.HasValue);
		_worldPacket.WriteBit(QuoteDuration.HasValue);
		_worldPacket.WriteInt32(ItemID);
		_worldPacket.WriteUInt32(DesiredDelay);

		if (TotalPrice.HasValue)
			_worldPacket.WriteUInt64(TotalPrice.Value);

		if (Quantity.HasValue)
			_worldPacket.WriteUInt32(Quantity.Value);

		if (QuoteDuration.HasValue)
			_worldPacket.WriteInt32(QuoteDuration.Value);
	}
}