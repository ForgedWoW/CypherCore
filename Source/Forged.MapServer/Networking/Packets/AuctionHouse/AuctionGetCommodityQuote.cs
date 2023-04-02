// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Addon;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionGetCommodityQuote : ClientPacket
{
    public ObjectGuid Auctioneer;
    public int ItemID;
    public uint Quantity;
    public AddOnInfo? TaintedBy;

    public AuctionGetCommodityQuote(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Auctioneer = WorldPacket.ReadPackedGuid();
        ItemID = WorldPacket.ReadInt32();
        Quantity = WorldPacket.ReadUInt32();

        if (WorldPacket.HasBit())
        {
            TaintedBy = new AddOnInfo();
            TaintedBy.Value.Read(WorldPacket);
        }
    }
}