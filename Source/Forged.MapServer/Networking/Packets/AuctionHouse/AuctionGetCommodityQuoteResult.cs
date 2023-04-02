// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionGetCommodityQuoteResult : ServerPacket
{
    public uint DesiredDelay;
    public int ItemID;
    public uint? Quantity;
    public int? QuoteDuration;
    public ulong? TotalPrice;
    public AuctionGetCommodityQuoteResult() : base(ServerOpcodes.AuctionGetCommodityQuoteResult) { }

    public override void Write()
    {
        WorldPacket.WriteBit(TotalPrice.HasValue);
        WorldPacket.WriteBit(Quantity.HasValue);
        WorldPacket.WriteBit(QuoteDuration.HasValue);
        WorldPacket.WriteInt32(ItemID);
        WorldPacket.WriteUInt32(DesiredDelay);

        if (TotalPrice.HasValue)
            WorldPacket.WriteUInt64(TotalPrice.Value);

        if (Quantity.HasValue)
            WorldPacket.WriteUInt32(Quantity.Value);

        if (QuoteDuration.HasValue)
            WorldPacket.WriteInt32(QuoteDuration.Value);
    }
}