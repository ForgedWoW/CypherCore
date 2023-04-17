// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

public struct AuctionListFilterSubClass
{
    public ulong InvTypeMask;
    public int ItemSubclass;

    public AuctionListFilterSubClass(WorldPacket data)
    {
        InvTypeMask = data.ReadUInt64();
        ItemSubclass = data.ReadInt32();
    }
}