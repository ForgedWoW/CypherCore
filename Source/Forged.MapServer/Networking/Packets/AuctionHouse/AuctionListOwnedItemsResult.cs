// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

public class AuctionListOwnedItemsResult : ServerPacket
{
    public List<AuctionItem> Items = new();
    public List<AuctionItem> SoldItems = new();
    public uint DesiredDelay;
    public bool HasMoreResults;

    public AuctionListOwnedItemsResult() : base(ServerOpcodes.AuctionListOwnedItemsResult) { }

    public override void Write()
    {
        _worldPacket.WriteInt32(Items.Count);
        _worldPacket.WriteInt32(SoldItems.Count);
        _worldPacket.WriteUInt32(DesiredDelay);
        _worldPacket.WriteBit(HasMoreResults);
        _worldPacket.FlushBits();

        foreach (var item in Items)
            item.Write(_worldPacket);

        foreach (var item in SoldItems)
            item.Write(_worldPacket);
    }
}