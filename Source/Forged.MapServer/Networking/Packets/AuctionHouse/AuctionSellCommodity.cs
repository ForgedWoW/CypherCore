// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Addon;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionSellCommodity : ClientPacket
{
    public ObjectGuid Auctioneer;
    public Array<AuctionItemForSale> Items = new(64);
    public uint RunTime;
    public AddOnInfo? TaintedBy;
    public ulong UnitPrice;
    public AuctionSellCommodity(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Auctioneer = WorldPacket.ReadPackedGuid();
        UnitPrice = WorldPacket.ReadUInt64();
        RunTime = WorldPacket.ReadUInt32();

        if (WorldPacket.HasBit())
            TaintedBy = new AddOnInfo();

        var itemCount = WorldPacket.ReadBits<uint>(6);

        TaintedBy?.Read(WorldPacket);

        for (var i = 0; i < itemCount; ++i)
            Items[i] = new AuctionItemForSale(WorldPacket);
    }
}