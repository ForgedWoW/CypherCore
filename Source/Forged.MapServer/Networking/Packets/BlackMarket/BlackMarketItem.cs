// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.BlackMarket;

public struct BlackMarketItem
{
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

    public uint MarketID;
    public uint SellerNPC;
    public ItemInstance Item;
    public uint Quantity;
    public ulong MinBid;
    public ulong MinIncrement;
    public ulong CurrentBid;
    public uint SecondsRemaining;
    public uint NumBids;
    public bool HighBid;
}