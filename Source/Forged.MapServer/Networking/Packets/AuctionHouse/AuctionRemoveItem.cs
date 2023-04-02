// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Addon;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionRemoveItem : ClientPacket
{
    public ObjectGuid Auctioneer;
    public uint AuctionID;
    public int ItemID;
    public AddOnInfo? TaintedBy;

    public AuctionRemoveItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Auctioneer = WorldPacket.ReadPackedGuid();
        AuctionID = WorldPacket.ReadUInt32();
        ItemID = WorldPacket.ReadInt32();

        if (WorldPacket.HasBit())
        {
            TaintedBy = new AddOnInfo();
            TaintedBy.Value.Read(WorldPacket);
        }
    }
}