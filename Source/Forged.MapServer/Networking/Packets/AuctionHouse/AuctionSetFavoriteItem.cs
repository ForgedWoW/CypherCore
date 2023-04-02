// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionSetFavoriteItem : ClientPacket
{
    public bool IsNotFavorite = true;
    public AuctionFavoriteInfo Item;
    public AuctionSetFavoriteItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        IsNotFavorite = WorldPacket.HasBit();
        Item = new AuctionFavoriteInfo(WorldPacket);
    }
}