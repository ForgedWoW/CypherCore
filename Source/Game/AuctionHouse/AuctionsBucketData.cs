// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game;

public class AuctionsBucketData
{
	public AuctionsBucketKey Key;

	// filter helpers
	public byte ItemClass;
	public byte ItemSubClass;
	public byte InventoryType;
	public AuctionHouseFilterMask QualityMask;
	public uint[] QualityCounts = new uint[(int)ItemQuality.Max];
	public ulong MinPrice;                                                                  // for sort
	public (uint Id, uint Count)[] ItemModifiedAppearanceId = new (uint Id, uint Count)[4]; // for uncollected search
	public byte RequiredLevel = 0;                                                          // for usable search
	public byte SortLevel = 0;
	public byte MinBattlePetLevel = 0;
	public byte MaxBattlePetLevel = 0;
	public string[] FullName = new string[(int)Locale.Total];

	public List<AuctionPosting> Auctions = new();

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
		readonly Locale _locale;
		readonly AuctionSortDef[] _sorts;
		readonly int _sortCount;

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

		long CompareColumns(AuctionHouseSortOrder column, AuctionsBucketData left, AuctionsBucketData right)
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
				default:
					break;
			}

			return 0;
		}
	}
}