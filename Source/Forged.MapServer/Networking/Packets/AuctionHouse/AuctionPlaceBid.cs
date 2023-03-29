// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Addon;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionPlaceBid : ClientPacket
{
    public ObjectGuid Auctioneer;
    public ulong BidAmount;
    public uint AuctionID;
    public AddOnInfo? TaintedBy;

    public AuctionPlaceBid(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Auctioneer = _worldPacket.ReadPackedGuid();
        AuctionID = _worldPacket.ReadUInt32();
        BidAmount = _worldPacket.ReadUInt64();

        if (_worldPacket.HasBit())
        {
            TaintedBy = new AddOnInfo();
            TaintedBy.Value.Read(_worldPacket);
        }
    }
}