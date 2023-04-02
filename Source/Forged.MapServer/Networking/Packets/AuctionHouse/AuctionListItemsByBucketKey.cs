// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Addon;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionListItemsByBucketKey : ClientPacket
{
    public ObjectGuid Auctioneer;
    public AuctionBucketKey BucketKey;
    public uint Offset;
    public Array<AuctionSortDef> Sorts = new(2);
    public AddOnInfo? TaintedBy;
    public sbyte Unknown830;
    public AuctionListItemsByBucketKey(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Auctioneer = _worldPacket.ReadPackedGuid();
        Offset = _worldPacket.ReadUInt32();
        Unknown830 = _worldPacket.ReadInt8();

        if (_worldPacket.HasBit())
            TaintedBy = new AddOnInfo();

        var sortCount = _worldPacket.ReadBits<uint>(2);

        for (var i = 0; i < sortCount; ++i)
            Sorts[i] = new AuctionSortDef(_worldPacket);

        BucketKey = new AuctionBucketKey(_worldPacket);

        TaintedBy?.Read(_worldPacket);
    }
}