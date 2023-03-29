// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

public class AuctionListFilterClass
{
    public int ItemClass;
    public Array<AuctionListFilterSubClass> SubClassFilters = new(31);

    public AuctionListFilterClass(WorldPacket data)
    {
        ItemClass = data.ReadInt32();
        var subClassFilterCount = data.ReadBits<uint>(5);

        for (var i = 0; i < subClassFilterCount; ++i)
            SubClassFilters[i] = new AuctionListFilterSubClass(data);
    }
}