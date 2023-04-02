// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionOwnerBidNotification : ServerPacket
{
    public ObjectGuid Bidder;
    public AuctionOwnerNotification Info;
    public ulong MinIncrement;

    public AuctionOwnerBidNotification() : base(ServerOpcodes.AuctionOwnerBidNotification) { }

    public override void Write()
    {
        Info.Write(WorldPacket);
        WorldPacket.WriteUInt64(MinIncrement);
        WorldPacket.WritePackedGuid(Bidder);
    }
}