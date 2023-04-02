// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

public class AuctionListItemsResult : ServerPacket
{
    public AuctionBucketKey BucketKey = new();
    public uint DesiredDelay;
    public bool HasMoreResults;
    public List<AuctionItem> Items = new();
    public AuctionHouseListType ListType;
    public uint TotalCount;
    public uint Unknown830;
    public AuctionListItemsResult() : base(ServerOpcodes.AuctionListItemsResult) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(Items.Count);
        WorldPacket.WriteUInt32(Unknown830);
        WorldPacket.WriteUInt32(TotalCount);
        WorldPacket.WriteUInt32(DesiredDelay);
        WorldPacket.WriteBits((int)ListType, 2);
        WorldPacket.WriteBit(HasMoreResults);
        WorldPacket.FlushBits();

        BucketKey.Write(WorldPacket);

        foreach (var item in Items)
            item.Write(WorldPacket);
    }
}