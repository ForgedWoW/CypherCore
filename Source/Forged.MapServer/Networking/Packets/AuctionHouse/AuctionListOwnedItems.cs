// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Addon;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionListOwnedItems : ClientPacket
{
    public ObjectGuid Auctioneer;
    public uint Offset;
    public AddOnInfo? TaintedBy;
    public Array<AuctionSortDef> Sorts = new(2);

    public AuctionListOwnedItems(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Auctioneer = _worldPacket.ReadPackedGuid();
        Offset = _worldPacket.ReadUInt32();

        if (_worldPacket.HasBit())
            TaintedBy = new AddOnInfo();

        var sortCount = _worldPacket.ReadBits<uint>(2);

        for (var i = 0; i < sortCount; ++i)
            Sorts[i] = new AuctionSortDef(_worldPacket);

        if (TaintedBy.HasValue)
            TaintedBy.Value.Read(_worldPacket);
    }
}