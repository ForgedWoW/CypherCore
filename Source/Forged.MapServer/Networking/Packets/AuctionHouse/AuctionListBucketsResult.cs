// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

public class AuctionListBucketsResult : ServerPacket
{
    public AuctionHouseBrowseMode BrowseMode;
    public List<BucketInfo> Buckets = new();
    public uint DesiredDelay;
    public bool HasMoreResults;
    public int Unknown830_0;
    public int Unknown830_1;
    public AuctionListBucketsResult() : base(ServerOpcodes.AuctionListBucketsResult) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(Buckets.Count);
        WorldPacket.WriteUInt32(DesiredDelay);
        WorldPacket.WriteInt32(Unknown830_0);
        WorldPacket.WriteInt32(Unknown830_1);
        WorldPacket.WriteBits((int)BrowseMode, 1);
        WorldPacket.WriteBit(HasMoreResults);
        WorldPacket.FlushBits();

        foreach (var bucketInfo in Buckets)
            bucketInfo.Write(WorldPacket);
    }
}