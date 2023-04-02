// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Addon;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionListBucketsByBucketKeys : ClientPacket
{
    public ObjectGuid Auctioneer;
    public Array<AuctionBucketKey> BucketKeys = new(100);
    public Array<AuctionSortDef> Sorts = new(2);
    public AddOnInfo? TaintedBy;
    public AuctionListBucketsByBucketKeys(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Auctioneer = _worldPacket.ReadPackedGuid();

        if (_worldPacket.HasBit())
            TaintedBy = new AddOnInfo();

        var bucketKeysCount = _worldPacket.ReadBits<uint>(7);
        var sortCount = _worldPacket.ReadBits<uint>(2);

        for (var i = 0; i < sortCount; ++i)
            Sorts[i] = new AuctionSortDef(_worldPacket);

        TaintedBy?.Read(_worldPacket);

        for (var i = 0; i < bucketKeysCount; ++i)
            BucketKeys[i] = new AuctionBucketKey(_worldPacket);
    }
}