// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Addon;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionListBiddedItems : ClientPacket
{
    public ObjectGuid Auctioneer;
    public List<uint> AuctionItemIDs = new();
    public uint Offset;
    public Array<AuctionSortDef> Sorts = new(2);
    public AddOnInfo? TaintedBy;

    public AuctionListBiddedItems(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Auctioneer = _worldPacket.ReadPackedGuid();
        Offset = _worldPacket.ReadUInt32();

        if (_worldPacket.HasBit())
            TaintedBy = new AddOnInfo();

        var auctionIDCount = _worldPacket.ReadBits<uint>(7);
        var sortCount = _worldPacket.ReadBits<uint>(2);

        for (var i = 0; i < sortCount; ++i)
            Sorts[i] = new AuctionSortDef(_worldPacket);

        TaintedBy?.Read(_worldPacket);

        for (var i = 0; i < auctionIDCount; ++i)
            AuctionItemIDs[i] = _worldPacket.ReadUInt32();
    }
}