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
        _worldPacket.WriteInt32(Items.Count);
        _worldPacket.WriteUInt32(Unknown830);
        _worldPacket.WriteUInt32(TotalCount);
        _worldPacket.WriteUInt32(DesiredDelay);
        _worldPacket.WriteBits((int)ListType, 2);
        _worldPacket.WriteBit(HasMoreResults);
        _worldPacket.FlushBits();

        BucketKey.Write(_worldPacket);

        foreach (var item in Items)
            item.Write(_worldPacket);
    }
}