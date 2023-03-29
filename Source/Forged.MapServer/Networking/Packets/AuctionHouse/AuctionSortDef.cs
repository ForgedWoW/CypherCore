// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

public struct AuctionSortDef
{
    public AuctionHouseSortOrder SortOrder;
    public bool ReverseSort;

    public AuctionSortDef(AuctionHouseSortOrder sortOrder, bool reverseSort)
    {
        SortOrder = sortOrder;
        ReverseSort = reverseSort;
    }

    public AuctionSortDef(WorldPacket data)
    {
        data.ResetBitPos();
        SortOrder = (AuctionHouseSortOrder)data.ReadBits<uint>(4);
        ReverseSort = data.HasBit();
    }
}