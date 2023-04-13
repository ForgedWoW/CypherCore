// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.AuctionHouse;
using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.AuctionHouse;

public class AuctionPosting
{
    public ObjectGuid Bidder;
    public ObjectGuid Owner;
    public ObjectGuid OwnerAccount;
    public ulong BidAmount { get; set; }
    public List<ObjectGuid> BidderHistory { get; set; } = new();
    public AuctionsBucketData Bucket { get; set; }
    public ulong BuyoutOrUnitPrice { get; set; }
    public ulong Deposit { get; set; }
    public DateTime EndTime { get; set; } = DateTime.MinValue;
    public uint Id { get; set; }
    public bool IsCommodity => Items.Count > 1 || Items[0].Template.MaxStackSize > 1;
    public List<Item> Items { get; set; } = new();
    public ulong MinBid { get; set; }
    public AuctionPostingServerFlag ServerFlags { get; set; }
    public DateTime StartTime { get; set; } = DateTime.MinValue;

    public uint TotalItemCount
    {
        get { return (uint)Items.Sum(item => item.Count); }
    }

    public static ulong CalculateMinIncrement(ulong bidAmount)
    {
        return MathFunctions.CalculatePct(bidAmount / MoneyConstants.Silver, 5) * MoneyConstants.Silver;
    }

    public void BuildAuctionItem(AuctionItem auctionItem, bool alwaysSendItem, bool sendKey, bool censorServerInfo, bool censorBidInfo)
    {
        // SMSG_AUCTION_LIST_BIDDER_ITEMS_RESULT, SMSG_AUCTION_LIST_ITEMS_RESULT (if not commodity), SMSG_AUCTION_LIST_OWNER_ITEMS_RESULT, SMSG_AUCTION_REPLICATE_RESPONSE (if not commodity)
        //auctionItem.Item - here to unify comment

        // all (not optional<>)
        auctionItem.Count = (int)TotalItemCount;
        auctionItem.Flags = Items[0].ItemData.DynamicFlags;
        auctionItem.AuctionID = Id;
        auctionItem.Owner = Owner;

        // prices set when filled
        if (IsCommodity)
        {
            if (alwaysSendItem)
                auctionItem.Item = new ItemInstance(Items[0]);

            auctionItem.UnitPrice = BuyoutOrUnitPrice;
        }
        else
        {
            auctionItem.Item = new ItemInstance(Items[0]);

            auctionItem.Charges = new[]
            {
                Items[0].GetSpellCharges(), Items[0].GetSpellCharges(1), Items[0].GetSpellCharges(2), Items[0].GetSpellCharges(3), Items[0].GetSpellCharges(4)
            }.Max();

            for (EnchantmentSlot enchantmentSlot = 0; enchantmentSlot < EnchantmentSlot.MaxInspected; enchantmentSlot++)
            {
                var enchantId = Items[0].GetEnchantmentId(enchantmentSlot);

                if (enchantId == 0)
                    continue;

                auctionItem.Enchantments.Add(new ItemEnchantData(enchantId, Items[0].GetEnchantmentDuration(enchantmentSlot), Items[0].GetEnchantmentCharges(enchantmentSlot), (byte)enchantmentSlot));
            }

            for (byte i = 0; i < Items[0].ItemData.Gems.Size(); ++i)
            {
                var gemData = Items[0].ItemData.Gems[i];

                if (gemData.ItemId != 0)
                {
                    ItemGemData gem = new()
                    {
                        Slot = i,
                        Item = new ItemInstance(gemData)
                    };

                    auctionItem.Gems.Add(gem);
                }
            }

            if (MinBid != 0)
                auctionItem.MinBid = MinBid;

            var minIncrement = CalculateMinIncrement();

            if (minIncrement != 0)
                auctionItem.MinIncrement = minIncrement;

            if (BuyoutOrUnitPrice != 0)
                auctionItem.BuyoutPrice = BuyoutOrUnitPrice;
        }

        // all (not optional<>)
        auctionItem.DurationLeft = (int)Math.Max((EndTime - GameTime.SystemTime).TotalMilliseconds, 0L);
        auctionItem.DeleteReason = 0;

        // SMSG_AUCTION_LIST_ITEMS_RESULT (only if owned)
        auctionItem.CensorServerSideInfo = censorServerInfo;
        auctionItem.ItemGuid = IsCommodity ? ObjectGuid.Empty : Items[0].GUID;
        auctionItem.OwnerAccountID = OwnerAccount;
        auctionItem.EndTime = (uint)Time.DateTimeToUnixTime(EndTime);

        // SMSG_AUCTION_LIST_BIDDER_ITEMS_RESULT, SMSG_AUCTION_LIST_ITEMS_RESULT (if has bid), SMSG_AUCTION_LIST_OWNER_ITEMS_RESULT, SMSG_AUCTION_REPLICATE_RESPONSE (if has bid)
        auctionItem.CensorBidInfo = censorBidInfo;

        if (!Bidder.IsEmpty)
        {
            auctionItem.Bidder = Bidder;
            auctionItem.BidAmount = BidAmount;
        }

        // SMSG_AUCTION_LIST_BIDDER_ITEMS_RESULT, SMSG_AUCTION_LIST_OWNER_ITEMS_RESULT, SMSG_AUCTION_REPLICATE_RESPONSE (if commodity)
        if (sendKey)
            auctionItem.AuctionBucketKey = new AuctionBucketKey(AuctionsBucketKey.ForItem(Items[0]));

        // all
        if (!Items[0].ItemData.Creator.Value.IsEmpty)
            auctionItem.Creator = Items[0].ItemData.Creator;
    }

    public ulong CalculateMinIncrement()
    {
        return CalculateMinIncrement(BidAmount);
    }

    public class Sorter : IComparer<AuctionPosting>
    {
        private readonly Locale _locale;
        private readonly int _sortCount;
        private readonly AuctionSortDef[] _sorts;

        public Sorter(Locale locale, AuctionSortDef[] sorts, int sortCount)
        {
            _locale = locale;
            _sorts = sorts;
            _sortCount = sortCount;
        }

        public int Compare(AuctionPosting left, AuctionPosting right)
        {
            for (var i = 0; i < _sortCount; ++i)
            {
                var ordering = CompareColumns(_sorts[i].SortOrder, left, right);

                if (ordering != 0)
                    return (ordering < 0).CompareTo(!_sorts[i].ReverseSort);
            }

            if (left == null || right == null)
                return -1;

            // Auctions are processed in LIFO order
            return left.StartTime != right.StartTime ? left.StartTime.CompareTo(right.StartTime) : left.Id.CompareTo(right.Id);
        }

        private long CompareColumns(AuctionHouseSortOrder column, AuctionPosting left, AuctionPosting right)
        {
            switch (column)
            {
                case AuctionHouseSortOrder.Price:
                {
                    var leftPrice = left.BuyoutOrUnitPrice != 0   ? left.BuyoutOrUnitPrice : left.BidAmount != 0   ? left.BidAmount : left.MinBid;
                    var rightPrice = right.BuyoutOrUnitPrice != 0 ? right.BuyoutOrUnitPrice : right.BidAmount != 0 ? right.BidAmount : right.MinBid;

                    return (long)(leftPrice - rightPrice);
                }
                case AuctionHouseSortOrder.Name:
                    return String.Compare(left.Bucket.FullName[(int)_locale], right.Bucket.FullName[(int)_locale], StringComparison.Ordinal);

                case AuctionHouseSortOrder.Level:
                {
                    var leftLevel = left.Items[0].GetModifier(ItemModifier.BattlePetSpeciesId) == 0 ? left.Bucket.SortLevel : (int)left.Items[0].GetModifier(ItemModifier.BattlePetLevel);
                    var rightLevel = right.Items[0].GetModifier(ItemModifier.BattlePetSpeciesId) == 0 ? right.Bucket.SortLevel : (int)right.Items[0].GetModifier(ItemModifier.BattlePetLevel);

                    return leftLevel - rightLevel;
                }
                case AuctionHouseSortOrder.Bid:
                    return (long)(left.BidAmount - right.BidAmount);

                case AuctionHouseSortOrder.Buyout:
                    return (long)(left.BuyoutOrUnitPrice - right.BuyoutOrUnitPrice);
            }

            return 0;
        }
    }
}