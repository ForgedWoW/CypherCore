// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking.Packets.AuctionHouse;
using Framework.Constants;

namespace Forged.MapServer.AuctionHouse;

public class AuctionsBucketData
{
    public List<AuctionPosting> Auctions = new();
    public string[] FullName = new string[(int)Locale.Total];
    public byte InventoryType;
    // filter helpers
    public byte ItemClass;

    public (uint Id, uint Count)[] ItemModifiedAppearanceId = new (uint Id, uint Count)[4];
    public byte ItemSubClass;
    public AuctionsBucketKey Key;
    public byte MaxBattlePetLevel = 0;
    public byte MinBattlePetLevel = 0;
    public ulong MinPrice;
    public uint[] QualityCounts = new uint[(int)ItemQuality.Max];
    public AuctionHouseFilterMask QualityMask;
    // for sort
    // for uncollected search
    public byte RequiredLevel = 0;                                                          // for usable search
    public byte SortLevel = 0;
    public void BuildBucketInfo(BucketInfo bucketInfo, Player player)
    {
        bucketInfo.Key = new AuctionBucketKey(Key);
        bucketInfo.MinPrice = MinPrice;
        bucketInfo.RequiredLevel = RequiredLevel;
        bucketInfo.TotalQuantity = 0;

        foreach (var auction in Auctions)
        {
            foreach (var item in auction.Items)
            {
                bucketInfo.TotalQuantity += (int)item.Count;

                if (Key.BattlePetSpeciesId != 0)
                {
                    var breedData = item.GetModifier(ItemModifier.BattlePetBreedData);
                    var breedId = breedData & 0xFFFFFF;
                    var quality = (byte)((breedData >> 24) & 0xFF);
                    var level = (byte)(item.GetModifier(ItemModifier.BattlePetLevel));

                    bucketInfo.MaxBattlePetQuality = bucketInfo.MaxBattlePetQuality.HasValue ? Math.Max(bucketInfo.MaxBattlePetQuality.Value, quality) : quality;
                    bucketInfo.MaxBattlePetLevel = bucketInfo.MaxBattlePetLevel.HasValue ? Math.Max(bucketInfo.MaxBattlePetLevel.Value, level) : level;
                    bucketInfo.BattlePetBreedID = (byte)breedId;
                }
            }

            bucketInfo.ContainsOwnerItem = bucketInfo.ContainsOwnerItem || auction.Owner == player.GUID;
        }

        bucketInfo.ContainsOnlyCollectedAppearances = true;

        foreach (var appearance in ItemModifiedAppearanceId)
            if (appearance.Item1 != 0)
            {
                bucketInfo.ItemModifiedAppearanceIDs.Add(appearance.Item1);

                if (!player.Session.CollectionMgr.HasItemAppearance(appearance.Item1).PermAppearance)
                    bucketInfo.ContainsOnlyCollectedAppearances = false;
            }
    }

    public class Sorter : IComparer<AuctionsBucketData>
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

        public int Compare(AuctionsBucketData left, AuctionsBucketData right)
        {
            for (var i = 0; i < _sortCount; ++i)
            {
                var ordering = CompareColumns(_sorts[i].SortOrder, left, right);

                if (ordering != 0)
                    return (ordering < 0).CompareTo(!_sorts[i].ReverseSort);
            }

            return left.Key != right.Key ? 1 : 0;
        }

        private long CompareColumns(AuctionHouseSortOrder column, AuctionsBucketData left, AuctionsBucketData right)
        {
            switch (column)
            {
                case AuctionHouseSortOrder.Price:
                case AuctionHouseSortOrder.Bid:
                case AuctionHouseSortOrder.Buyout:
                    return (long)(left.MinPrice - right.MinPrice);
                case AuctionHouseSortOrder.Name:
                    return left.FullName[(int)_locale].CompareTo(right.FullName[(int)_locale]);
                case AuctionHouseSortOrder.Level:
                    return left.SortLevel - right.SortLevel;
                
            }

            return 0;
        }
    }
}