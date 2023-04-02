// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.BlackMarket;

internal class BlackMarketBidOnItem : ClientPacket
{
    public ulong BidAmount;
    public ObjectGuid Guid;
    public ItemInstance Item = new();
    public uint MarketID;
    public BlackMarketBidOnItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Guid = _worldPacket.ReadPackedGuid();
        MarketID = _worldPacket.ReadUInt32();
        BidAmount = _worldPacket.ReadUInt64();
        Item.Read(_worldPacket);
    }
}