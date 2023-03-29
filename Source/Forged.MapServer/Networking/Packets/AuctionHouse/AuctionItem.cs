// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

public class AuctionItem
{
    public ItemInstance Item;
    public int Count;
    public int Charges;
    public List<ItemEnchantData> Enchantments = new();
    public uint Flags;
    public uint AuctionID;
    public ObjectGuid Owner;
    public ulong? MinBid;
    public ulong? MinIncrement;
    public ulong? BuyoutPrice;
    public ulong? UnitPrice;
    public int DurationLeft;
    public byte DeleteReason;
    public bool CensorServerSideInfo;
    public bool CensorBidInfo;
    public ObjectGuid ItemGuid;
    public ObjectGuid OwnerAccountID;
    public uint EndTime;
    public ObjectGuid? Bidder;
    public ulong? BidAmount;
    public List<ItemGemData> Gems = new();
    public AuctionBucketKey AuctionBucketKey;
    public ObjectGuid? Creator;

    public void Write(WorldPacket data)
    {
        data.WriteBit(Item != null);
        data.WriteBits(Enchantments.Count, 4);
        data.WriteBits(Gems.Count, 2);
        data.WriteBit(MinBid.HasValue);
        data.WriteBit(MinIncrement.HasValue);
        data.WriteBit(BuyoutPrice.HasValue);
        data.WriteBit(UnitPrice.HasValue);
        data.WriteBit(CensorServerSideInfo);
        data.WriteBit(CensorBidInfo);
        data.WriteBit(AuctionBucketKey != null);
        data.WriteBit(Creator.HasValue);

        if (!CensorBidInfo)
        {
            data.WriteBit(Bidder.HasValue);
            data.WriteBit(BidAmount.HasValue);
        }

        data.FlushBits();

        if (Item != null)
            Item.Write(data);

        data.WriteInt32(Count);
        data.WriteInt32(Charges);
        data.WriteUInt32(Flags);
        data.WriteUInt32(AuctionID);
        data.WritePackedGuid(Owner);
        data.WriteInt32(DurationLeft);
        data.WriteUInt8(DeleteReason);

        foreach (var enchant in Enchantments)
            enchant.Write(data);

        if (MinBid.HasValue)
            data.WriteUInt64(MinBid.Value);

        if (MinIncrement.HasValue)
            data.WriteUInt64(MinIncrement.Value);

        if (BuyoutPrice.HasValue)
            data.WriteUInt64(BuyoutPrice.Value);

        if (UnitPrice.HasValue)
            data.WriteUInt64(UnitPrice.Value);

        if (!CensorServerSideInfo)
        {
            data.WritePackedGuid(ItemGuid);
            data.WritePackedGuid(OwnerAccountID);
            data.WriteUInt32(EndTime);
        }

        if (Creator.HasValue)
            data.WritePackedGuid(Creator.Value);

        if (!CensorBidInfo)
        {
            if (Bidder.HasValue)
                data.WritePackedGuid(Bidder.Value);

            if (BidAmount.HasValue)
                data.WriteUInt64(BidAmount.Value);
        }

        foreach (var gem in Gems)
            gem.Write(data);

        if (AuctionBucketKey != null)
            AuctionBucketKey.Write(data);
    }
}