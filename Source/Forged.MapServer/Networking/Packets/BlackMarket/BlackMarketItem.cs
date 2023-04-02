// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.BlackMarket;

public struct BlackMarketItem
{
    public ulong CurrentBid;

    public bool HighBid;

    public ItemInstance Item;

    public uint MarketID;

    public ulong MinBid;

    public ulong MinIncrement;

    public uint NumBids;

    public uint Quantity;

    public uint SecondsRemaining;

    public uint SellerNPC;

    public void Read(WorldPacket data)
    {
        MarketID = data.ReadUInt32();
        SellerNPC = data.ReadUInt32();
        Item.Read(data);
        Quantity = data.ReadUInt32();
        MinBid = data.ReadUInt64();
        MinIncrement = data.ReadUInt64();
        CurrentBid = data.ReadUInt64();
        SecondsRemaining = data.ReadUInt32();
        NumBids = data.ReadUInt32();
        HighBid = data.HasBit();
    }

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(MarketID);
        data.WriteUInt32(SellerNPC);
        data.WriteUInt32(Quantity);
        data.WriteUInt64(MinBid);
        data.WriteUInt64(MinIncrement);
        data.WriteUInt64(CurrentBid);
        data.WriteUInt32(SecondsRemaining);
        data.WriteUInt32(NumBids);
        Item.Write(data);
        data.WriteBit(HighBid);
        data.FlushBits();
    }
}