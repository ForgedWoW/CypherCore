// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Forged.RealmServer.BattlePets;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Mails;
using Forged.RealmServer.Networking.Packets;
using Forged.RealmServer.Scripting.Interfaces.IAuctionHouse;

namespace Forged.RealmServer;

public class AuctionHouseObject
{
	readonly AuctionHouseRecord _auctionHouse;
	readonly SortedList<uint, AuctionPosting> _itemsByAuctionId = new();               // ordered for replicate
	readonly SortedDictionary<AuctionsBucketKey, AuctionsBucketData> _buckets = new(); // ordered for search by itemid only
	readonly Dictionary<ObjectGuid, CommodityQuote> _commodityQuotes = new();
	readonly MultiMap<ObjectGuid, uint> _playerOwnedAuctions = new();
	readonly MultiMap<ObjectGuid, uint> _playerBidderAuctions = new();

	// Map of throttled players for GetAll, and throttle expiry time
	// Stored here, rather than player object to maintain persistence after logout
	readonly Dictionary<ObjectGuid, PlayerReplicateThrottleData> _replicateThrottleMap = new();

	public AuctionHouseObject(uint auctionHouseId)
	{
		_auctionHouse = CliDB.AuctionHouseStorage.LookupByKey(auctionHouseId);
	}

	public uint GetAuctionHouseId()
	{
		return _auctionHouse.Id;
	}

	public AuctionPosting GetAuction(uint auctionId)
	{
		return _itemsByAuctionId.LookupByKey(auctionId);
	}

	public void AddAuction(SQLTransaction trans, AuctionPosting auction)
	{
		var key = AuctionsBucketKey.ForItem(auction.Items[0]);

		var bucket = _buckets.LookupByKey(key);

		if (bucket == null)
		{
			// we don't have any item for this key yet, create new bucket
			bucket = new AuctionsBucketData();
			bucket.Key = key;

			var itemTemplate = auction.Items[0].Template;
			bucket.ItemClass = (byte)itemTemplate.Class;
			bucket.ItemSubClass = (byte)itemTemplate.SubClass;
			bucket.InventoryType = (byte)itemTemplate.InventoryType;
			bucket.RequiredLevel = (byte)auction.Items[0].GetRequiredLevel();

			switch (itemTemplate.Class)
			{
				case ItemClass.Weapon:
				case ItemClass.Armor:
					bucket.SortLevel = (byte)key.ItemLevel;

					break;
				case ItemClass.Container:
					bucket.SortLevel = (byte)itemTemplate.ContainerSlots;

					break;
				case ItemClass.Gem:
				case ItemClass.ItemEnhancement:
					bucket.SortLevel = (byte)itemTemplate.BaseItemLevel;

					break;
				case ItemClass.Consumable:
					bucket.SortLevel = Math.Max((byte)1, bucket.RequiredLevel);

					break;
				case ItemClass.Miscellaneous:
				case ItemClass.BattlePets:
					bucket.SortLevel = 1;

					break;
				case ItemClass.Recipe:
					bucket.SortLevel = (byte)((ItemSubClassRecipe)itemTemplate.SubClass != ItemSubClassRecipe.Book ? itemTemplate.RequiredSkillRank : (uint)itemTemplate.BaseRequiredLevel);

					break;
				default:
					break;
			}

			for (var locale = Locale.enUS; locale < Locale.Total; ++locale)
			{
				if (locale == Locale.None)
					continue;

				bucket.FullName[(int)locale] = auction.Items[0].GetName(locale);
			}

			_buckets.Add(key, bucket);
		}

		// update cache fields
		var priceToDisplay = auction.BuyoutOrUnitPrice != 0 ? auction.BuyoutOrUnitPrice : auction.BidAmount;

		if (bucket.MinPrice == 0 || priceToDisplay < bucket.MinPrice)
			bucket.MinPrice = priceToDisplay;

		var itemModifiedAppearance = auction.Items[0].GetItemModifiedAppearance();

		if (itemModifiedAppearance != null)
		{
			var index = 0;

			for (var i = 0; i < bucket.ItemModifiedAppearanceId.Length; ++i)
				if (bucket.ItemModifiedAppearanceId[i].Id == itemModifiedAppearance.Id)
				{
					index = i;

					break;
				}

			bucket.ItemModifiedAppearanceId[index] = (itemModifiedAppearance.Id, bucket.ItemModifiedAppearanceId[index].Item2 + 1);
		}

		uint quality;

		if (auction.Items[0].GetModifier(ItemModifier.BattlePetSpeciesId) == 0)
		{
			quality = (byte)auction.Items[0].Quality;
		}
		else
		{
			quality = (auction.Items[0].GetModifier(ItemModifier.BattlePetBreedData) >> 24) & 0xFF;

			foreach (var item in auction.Items)
			{
				var battlePetLevel = (byte)item.GetModifier(ItemModifier.BattlePetLevel);

				if (bucket.MinBattlePetLevel == 0)
					bucket.MinBattlePetLevel = battlePetLevel;
				else if (bucket.MinBattlePetLevel > battlePetLevel)
					bucket.MinBattlePetLevel = battlePetLevel;

				bucket.MaxBattlePetLevel = Math.Max(bucket.MaxBattlePetLevel, battlePetLevel);
				bucket.SortLevel = bucket.MaxBattlePetLevel;
			}
		}

		bucket.QualityMask |= (AuctionHouseFilterMask)(1 << ((int)quality + 4));
		++bucket.QualityCounts[quality];

		if (trans != null)
		{
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_AUCTION);
			stmt.AddValue(0, auction.Id);
			stmt.AddValue(1, _auctionHouse.Id);
			stmt.AddValue(2, auction.Owner.Counter);
			stmt.AddValue(3, ObjectGuid.Empty.Counter);
			stmt.AddValue(4, auction.MinBid);
			stmt.AddValue(5, auction.BuyoutOrUnitPrice);
			stmt.AddValue(6, auction.Deposit);
			stmt.AddValue(7, auction.BidAmount);
			stmt.AddValue(8, Time.DateTimeToUnixTime(auction.StartTime));
			stmt.AddValue(9, Time.DateTimeToUnixTime(auction.EndTime));
			stmt.AddValue(10, (byte)auction.ServerFlags);
			trans.Append(stmt);

			foreach (var item in auction.Items)
			{
				stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_AUCTION_ITEMS);
				stmt.AddValue(0, auction.Id);
				stmt.AddValue(1, item.GUID.Counter);
				trans.Append(stmt);
			}
		}

		foreach (var item in auction.Items)
			Global.AuctionHouseMgr.AddAItem(item);

		auction.Bucket = bucket;
		_playerOwnedAuctions.Add(auction.Owner, auction.Id);

		foreach (var bidder in auction.BidderHistory)
			_playerBidderAuctions.Add(bidder, auction.Id);

		_itemsByAuctionId[auction.Id] = auction;

		AuctionPosting.Sorter insertSorter = new(Locale.enUS,
												new AuctionSortDef[]
												{
													new(AuctionHouseSortOrder.Price, false)
												},
												1);

		var auctionIndex = bucket.Auctions.BinarySearch(auction, insertSorter);

		if (auctionIndex < 0)
			auctionIndex = ~auctionIndex;

		bucket.Auctions.Insert(auctionIndex, auction);

		Global.ScriptMgr.ForEach<IAuctionHouseOnAuctionAdd>(p => p.OnAuctionAdd(this, auction));
	}

	public void RemoveAuction(SQLTransaction trans, AuctionPosting auction, AuctionPosting auctionPosting = null)
	{
		var bucket = auction.Bucket;

		bucket.Auctions.RemoveAll(auct => auct.Id == auction.Id);

		if (!bucket.Auctions.Empty())
		{
			// update cache fields
			var priceToDisplay = auction.BuyoutOrUnitPrice != 0 ? auction.BuyoutOrUnitPrice : auction.BidAmount;

			if (bucket.MinPrice == priceToDisplay)
			{
				bucket.MinPrice = ulong.MaxValue;

				foreach (var remainingAuction in bucket.Auctions)
					bucket.MinPrice = Math.Min(bucket.MinPrice, remainingAuction.BuyoutOrUnitPrice != 0 ? remainingAuction.BuyoutOrUnitPrice : remainingAuction.BidAmount);
			}

			var itemModifiedAppearance = auction.Items[0].GetItemModifiedAppearance();

			if (itemModifiedAppearance != null)
			{
				var index = -1;

				for (var i = 0; i < bucket.ItemModifiedAppearanceId.Length; ++i)
					if (bucket.ItemModifiedAppearanceId[i].Item1 == itemModifiedAppearance.Id)
					{
						index = i;

						break;
					}

				if (index != -1)
					if (--bucket.ItemModifiedAppearanceId[index].Count == 0)
						bucket.ItemModifiedAppearanceId[index].Id = 0;
			}

			uint quality;

			if (auction.Items[0].GetModifier(ItemModifier.BattlePetSpeciesId) == 0)
			{
				quality = (uint)auction.Items[0].Quality;
			}
			else
			{
				quality = (auction.Items[0].GetModifier(ItemModifier.BattlePetBreedData) >> 24) & 0xFF;
				bucket.MinBattlePetLevel = 0;
				bucket.MaxBattlePetLevel = 0;

				foreach (var remainingAuction in bucket.Auctions)
				{
					foreach (var item in remainingAuction.Items)
					{
						var battlePetLevel = (byte)item.GetModifier(ItemModifier.BattlePetLevel);

						if (bucket.MinBattlePetLevel == 0)
							bucket.MinBattlePetLevel = battlePetLevel;
						else if (bucket.MinBattlePetLevel > battlePetLevel)
							bucket.MinBattlePetLevel = battlePetLevel;

						bucket.MaxBattlePetLevel = Math.Max(bucket.MaxBattlePetLevel, battlePetLevel);
					}
				}
			}

			if (--bucket.QualityCounts[quality] == 0)
				bucket.QualityMask &= (AuctionHouseFilterMask)(~(1 << ((int)quality + 4)));
		}
		else
		{
			_buckets.Remove(bucket.Key);
		}

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_AUCTION);
		stmt.AddValue(0, auction.Id);
		trans.Append(stmt);

		foreach (var item in auction.Items)
			Global.AuctionHouseMgr.RemoveAItem(item.GUID);

		Global.ScriptMgr.ForEach<IAuctionHouseOnAcutionRemove>(p => p.OnAuctionRemove(this, auction));

		_playerOwnedAuctions.Remove(auction.Owner, auction.Id);

		foreach (var bidder in auction.BidderHistory)
			_playerBidderAuctions.Remove(bidder, auction.Id);

		_itemsByAuctionId.Remove(auction.Id);
	}

	public void Update()
	{
		var curTime = GameTime.GetSystemTime();
		var curTimeSteady = GameTime.Now();
		///- Handle expired auctions

		// Clear expired throttled players
		foreach (var key in _replicateThrottleMap.Keys.ToList())
			if (_replicateThrottleMap[key].NextAllowedReplication <= curTimeSteady)
				_replicateThrottleMap.Remove(key);

		foreach (var key in _commodityQuotes.Keys.ToList())
			if (_commodityQuotes[key].ValidTo < curTimeSteady)
				_commodityQuotes.Remove(key);

		if (_itemsByAuctionId.Empty())
			return;

		SQLTransaction trans = new();

		foreach (var auction in _itemsByAuctionId.Values.ToList())
		{
			///- filter auctions expired on next update
			if (auction.EndTime > curTime.AddMinutes(1))
				continue;

			///- Either cancel the auction if there was no bidder
			if (auction.Bidder.IsEmpty)
			{
				SendAuctionExpired(auction, trans);
				Global.ScriptMgr.ForEach<IAuctionHouseOnAuctionExpire>(p => p.OnAuctionExpire(this, auction));
			}
			///- Or perform the transaction
			else
			{
				//we should send an "item sold" message if the seller is online
				//we send the item to the winner
				//we send the money to the seller
				SendAuctionWon(auction, null, trans);
				SendAuctionSold(auction, null, trans);
				Global.ScriptMgr.ForEach<IAuctionHouseOnAuctionSuccessful>(p => p.OnAuctionSuccessful(this, auction));
			}

			///- In any case clear the auction
			RemoveAuction(trans, auction);
		}

		// Run DB changes
		DB.Characters.CommitTransaction(trans);
	}

	public void BuildListAuctionItems(AuctionListItemsResult listItemsResult, Player player, uint itemId, uint offset, AuctionSortDef[] sorts, int sortCount)
	{
		var sorter = new AuctionPosting.Sorter(player.Session.SessionDbcLocale, sorts, sortCount);
		var builder = new AuctionsResultBuilder<AuctionPosting>(offset, sorter, AuctionHouseResultLimits.Items);

		listItemsResult.TotalCount = 0;
		var bucketData = _buckets.LookupByKey(new AuctionsBucketKey(itemId, 0, 0, 0));

		if (bucketData != null)
			foreach (var auction in bucketData.Auctions)
			{
				builder.AddItem(auction);

				foreach (var item in auction.Items)
					listItemsResult.TotalCount += item.Count;
			}

		foreach (var resultAuction in builder.GetResultRange())
		{
			AuctionItem auctionItem = new();

			resultAuction.BuildAuctionItem(auctionItem,
											false,
											true,
											resultAuction.OwnerAccount != player.Session.AccountGUID,
											resultAuction.Bidder.IsEmpty);

			listItemsResult.Items.Add(auctionItem);
		}

		listItemsResult.HasMoreResults = builder.HasMoreResults();
	}

	public void BuildListOwnedItems(AuctionListOwnedItemsResult listOwnerItemsResult, Player player, uint offset, AuctionSortDef[] sorts, int sortCount)
	{
		// always full list
		List<AuctionPosting> auctions = new();

		foreach (var auctionId in _playerOwnedAuctions.LookupByKey(player.GUID))
		{
			var auction = _itemsByAuctionId.LookupByKey(auctionId);

			if (auction != null)
				auctions.Add(auction);
		}

		AuctionPosting.Sorter sorter = new(player.Session.SessionDbcLocale, sorts, sortCount);
		auctions.Sort(sorter);

		foreach (var resultAuction in auctions)
		{
			AuctionItem auctionItem = new();
			resultAuction.BuildAuctionItem(auctionItem, true, true, false, false);
			listOwnerItemsResult.Items.Add(auctionItem);
		}

		listOwnerItemsResult.HasMoreResults = false;
	}

	public void BuildReplicate(AuctionReplicateResponse replicateResponse, Player player, uint global, uint cursor, uint tombstone, uint count)
	{
		var curTime = GameTime.Now();

		var throttleData = _replicateThrottleMap.LookupByKey(player.GUID);

		if (throttleData == null)
		{
			throttleData = new PlayerReplicateThrottleData();
			throttleData.NextAllowedReplication = curTime + TimeSpan.FromSeconds(WorldConfig.GetIntValue(WorldCfg.AuctionReplicateDelay));
			throttleData.Global = Global.AuctionHouseMgr.GenerateReplicationId;
		}
		else
		{
			if (throttleData.Global != global || throttleData.Cursor != cursor || throttleData.Tombstone != tombstone)
				return;

			if (!throttleData.IsReplicationInProgress() && throttleData.NextAllowedReplication > curTime)
				return;
		}

		if (_itemsByAuctionId.Empty() || count == 0)
			return;

		var keyIndex = _itemsByAuctionId.IndexOfKey(cursor);

		foreach (var pair in _itemsByAuctionId.Skip(keyIndex))
		{
			AuctionItem auctionItem = new();
			pair.Value.BuildAuctionItem(auctionItem, false, true, true, pair.Value.Bidder.IsEmpty);
			replicateResponse.Items.Add(auctionItem);

			if (--count == 0)
				break;
		}

		replicateResponse.ChangeNumberGlobal = throttleData.Global;
		replicateResponse.ChangeNumberCursor = throttleData.Cursor = !replicateResponse.Items.Empty() ? replicateResponse.Items.Last().AuctionID : 0;
		replicateResponse.ChangeNumberTombstone = throttleData.Tombstone = count == 0 ? _itemsByAuctionId.First().Key : 0;
		_replicateThrottleMap[player.GUID] = throttleData;
	}

	public ulong CalculateAuctionHouseCut(ulong bidAmount)
	{
		return (ulong)Math.Max((long)(MathFunctions.CalculatePct(bidAmount, _auctionHouse.ConsignmentRate) * WorldConfig.GetFloatValue(WorldCfg.RateAuctionCut)), 0);
	}

	public CommodityQuote CreateCommodityQuote(Player player, uint itemId, uint quantity)
	{
		var itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);

		if (itemTemplate == null)
			return null;

		var bucketData = _buckets.LookupByKey(AuctionsBucketKey.ForCommodity(itemTemplate));

		if (bucketData == null)
			return null;

		ulong totalPrice = 0;
		var remainingQuantity = quantity;

		foreach (var auction in bucketData.Auctions)
		{
			foreach (var auctionItem in auction.Items)
			{
				if (auctionItem.Count >= remainingQuantity)
				{
					totalPrice += auction.BuyoutOrUnitPrice * remainingQuantity;
					remainingQuantity = 0;

					break;
				}

				totalPrice += auction.BuyoutOrUnitPrice * auctionItem.Count;
				remainingQuantity -= auctionItem.Count;
			}
		}

		// not enough items on auction house
		if (remainingQuantity != 0)
			return null;

		if (!player.HasEnoughMoney(totalPrice))
			return null;

		var quote = _commodityQuotes[player.GUID];
		quote.TotalPrice = totalPrice;
		quote.Quantity = quantity;
		quote.ValidTo = GameTime.Now() + TimeSpan.FromSeconds(30);

		return quote;
	}

	public void CancelCommodityQuote(ObjectGuid guid)
	{
		_commodityQuotes.Remove(guid);
	}

	public bool BuyCommodity(SQLTransaction trans, Player player, uint itemId, uint quantity, TimeSpan delayForNextAction)
	{
		var itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);

		if (itemTemplate == null)
			return false;

		var bucketItr = _buckets.LookupByKey(AuctionsBucketKey.ForCommodity(itemTemplate));

		if (bucketItr == null)
		{
			player.Session.SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

			return false;
		}

		var quote = _commodityQuotes.LookupByKey(player.GUID);

		if (quote == null)
		{
			player.Session.SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

			return false;
		}

		ulong totalPrice = 0;
		var remainingQuantity = quantity;
		List<AuctionPosting> auctions = new();

		for (var i = 0; i < bucketItr.Auctions.Count;)
		{
			var auction = bucketItr.Auctions[i++];
			auctions.Add(auction);

			foreach (var auctionItem in auction.Items)
			{
				if (auctionItem.Count >= remainingQuantity)
				{
					totalPrice += auction.BuyoutOrUnitPrice * remainingQuantity;
					remainingQuantity = 0;
					i = bucketItr.Auctions.Count;

					break;
				}

				totalPrice += auction.BuyoutOrUnitPrice * auctionItem.Count;
				remainingQuantity -= auctionItem.Count;
			}
		}

		// not enough items on auction house
		if (remainingQuantity != 0)
		{
			player.Session.SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

			return false;
		}

		// something was bought between creating quote and finalizing transaction
		// but we allow lower price if new items were posted at lower price
		if (totalPrice > quote.TotalPrice)
		{
			player.Session.SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

			return false;
		}

		if (!player.HasEnoughMoney(totalPrice))
		{
			player.Session.SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

			return false;
		}

		ObjectGuid? uniqueSeller = new();

		// prepare items
		List<MailedItemsBatch> items = new();
		items.Add(new MailedItemsBatch());

		remainingQuantity = quantity;
		List<int> removedItemsFromAuction = new();

		for (var i = 0; i < bucketItr.Auctions.Count;)
		{
			var auction = bucketItr.Auctions[i++];

			if (!uniqueSeller.HasValue)
				uniqueSeller = auction.Owner;
			else if (uniqueSeller != auction.Owner)
				uniqueSeller = ObjectGuid.Empty;

			uint boughtFromAuction = 0;
			var removedItems = 0;

			foreach (var auctionItem in auction.Items)
			{
				var itemsBatch = items.Last();

				if (itemsBatch.IsFull())
				{
					items.Add(new MailedItemsBatch());
					itemsBatch = items.Last();
				}

				if (auctionItem.Count >= remainingQuantity)
				{
					var clonedItem = auctionItem.CloneItem(remainingQuantity, player);

					if (!clonedItem)
					{
						player.Session.SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

						return false;
					}

					auctionItem.SetCount(auctionItem.Count - remainingQuantity);
					auctionItem.FSetState(ItemUpdateState.Changed);
					auctionItem.SaveToDB(trans);
					itemsBatch.AddItem(clonedItem, auction.BuyoutOrUnitPrice);
					boughtFromAuction += remainingQuantity;
					remainingQuantity = 0;
					i = bucketItr.Auctions.Count;

					break;
				}

				itemsBatch.AddItem(auctionItem, auction.BuyoutOrUnitPrice);
				boughtFromAuction += auctionItem.Count;
				remainingQuantity -= auctionItem.Count;
				++removedItems;
			}

			removedItemsFromAuction.Add(removedItems);

			if (player.Session.HasPermission(RBACPermissions.LogGmTrade))
			{
				var bidderAccId = player.Session.AccountId;

				if (!Global.CharacterCacheStorage.GetCharacterNameByGuid(auction.Owner, out var ownerName))
					ownerName = Global.ObjectMgr.GetCypherString(CypherStrings.Unknown);

				Log.outCommand(bidderAccId,
								$"GM {player.GetName()} (Account: {bidderAccId}) bought commodity in auction: {items[0].Items[0].GetName(Global.WorldMgr.DefaultDbcLocale)} (Entry: {items[0].Items[0].Entry} " +
								$"Count: {boughtFromAuction}) and pay money: {auction.BuyoutOrUnitPrice * boughtFromAuction}. Original owner {ownerName} (Account: {Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(auction.Owner)})");
			}

			var auctionHouseCut = CalculateAuctionHouseCut(auction.BuyoutOrUnitPrice * boughtFromAuction);
			var depositPart = Global.AuctionHouseMgr.GetCommodityAuctionDeposit(items[0].Items[0].Template, (auction.EndTime - auction.StartTime), boughtFromAuction);
			var profit = auction.BuyoutOrUnitPrice * boughtFromAuction + depositPart - auctionHouseCut;

			var owner = Global.ObjAccessor.FindConnectedPlayer(auction.Owner);

			if (owner != null)
			{
				owner.UpdateCriteria(CriteriaType.MoneyEarnedFromAuctions, profit);
				owner.UpdateCriteria(CriteriaType.HighestAuctionSale, profit);
				owner.Session.SendAuctionClosedNotification(auction, (float)WorldConfig.GetIntValue(WorldCfg.MailDeliveryDelay), true);
			}

			new MailDraft(Global.AuctionHouseMgr.BuildCommodityAuctionMailSubject(AuctionMailType.Sold, itemId, boughtFromAuction),
						Global.AuctionHouseMgr.BuildAuctionSoldMailBody(player.GUID, auction.BuyoutOrUnitPrice * boughtFromAuction, boughtFromAuction, (uint)depositPart, auctionHouseCut))
				.AddMoney(profit)
				.SendMailTo(trans, new MailReceiver(Global.ObjAccessor.FindConnectedPlayer(auction.Owner), auction.Owner), new MailSender(this), MailCheckMask.Copied, WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay));
		}

		player.ModifyMoney(-(long)totalPrice);
		player.SaveGoldToDB(trans);

		foreach (var batch in items)
		{
			MailDraft mail = new(Global.AuctionHouseMgr.BuildCommodityAuctionMailSubject(AuctionMailType.Won, itemId, batch.Quantity),
								Global.AuctionHouseMgr.BuildAuctionWonMailBody(uniqueSeller.Value, batch.TotalPrice, batch.Quantity));

			for (var i = 0; i < batch.ItemsCount; ++i)
			{
				var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_AUCTION_ITEMS_BY_ITEM);
				stmt.AddValue(0, batch.Items[i].GUID.Counter);
				trans.Append(stmt);

				batch.Items[i].SetOwnerGUID(player.GUID);
				batch.Items[i].SaveToDB(trans);
				mail.AddItem(batch.Items[i]);
			}

			mail.SendMailTo(trans, player, new MailSender(this), MailCheckMask.Copied);
		}

		AuctionWonNotification packet = new();
		packet.Info.Initialize(auctions[0], items[0].Items[0]);
		player.SendPacket(packet);

		for (var i = 0; i < auctions.Count; ++i)
			if (removedItemsFromAuction[i] == auctions[i].Items.Count)
			{
				RemoveAuction(trans, auctions[i]); // bought all items
			}
			else if (removedItemsFromAuction[i] != 0)
			{
				var lastRemovedItemIndex = removedItemsFromAuction[i];

				for (var c = 0; c != removedItemsFromAuction[i]; ++c)
					Global.AuctionHouseMgr.RemoveAItem(auctions[i].Items[c].GUID);

				auctions[i].Items.RemoveRange(0, lastRemovedItemIndex);
			}

		return true;
	}

	// this function notified old bidder that his bid is no longer highest
	public void SendAuctionOutbid(AuctionPosting auction, ObjectGuid newBidder, ulong newBidAmount, SQLTransaction trans)
	{
		var oldBidder = Global.ObjAccessor.FindConnectedPlayer(auction.Bidder);

		// old bidder exist
		if ((oldBidder || Global.CharacterCacheStorage.HasCharacterCacheEntry(auction.Bidder))) // && !sAuctionBotConfig.IsBotChar(auction.Bidder))
		{
			if (oldBidder)
			{
				AuctionOutbidNotification packet = new();
				packet.BidAmount = newBidAmount;
				packet.MinIncrement = AuctionPosting.CalculateMinIncrement(newBidAmount);
				packet.Info.AuctionID = auction.Id;
				packet.Info.Bidder = newBidder;
				packet.Info.Item = new ItemInstance(auction.Items[0]);
				oldBidder.SendPacket(packet);
			}

			new MailDraft(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Outbid, auction), "")
				.AddMoney(auction.BidAmount)
				.SendMailTo(trans, new MailReceiver(oldBidder, auction.Bidder), new MailSender(this), MailCheckMask.Copied);
		}
	}

	public void SendAuctionWon(AuctionPosting auction, Player bidder, SQLTransaction trans)
	{
		uint bidderAccId;

		if (!bidder)
			bidder = Global.ObjAccessor.FindConnectedPlayer(auction.Bidder); // try lookup bidder when called from .Update

		// data for gm.log
		var bidderName = "";
		var logGmTrade = auction.ServerFlags.HasFlag(AuctionPostingServerFlag.GmLogBuyer);

		if (bidder)
		{
			bidderAccId = bidder.Session.AccountId;
			bidderName = bidder.GetName();
		}
		else
		{
			bidderAccId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(auction.Bidder);

			if (logGmTrade && !Global.CharacterCacheStorage.GetCharacterNameByGuid(auction.Bidder, out bidderName))
				bidderName = Global.ObjectMgr.GetCypherString(CypherStrings.Unknown);
		}

		if (logGmTrade)
		{
			if (!Global.CharacterCacheStorage.GetCharacterNameByGuid(auction.Owner, out var ownerName))
				ownerName = Global.ObjectMgr.GetCypherString(CypherStrings.Unknown);

			var ownerAccId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(auction.Owner);

			Log.outCommand(bidderAccId,
							$"GM {bidderName} (Account: {bidderAccId}) won item in auction: {auction.Items[0].GetName(Global.WorldMgr.DefaultDbcLocale)} (Entry: {auction.Items[0].Entry}" +
							$" Count: {auction.TotalItemCount}) and pay money: {auction.BidAmount}. Original owner {ownerName} (Account: {ownerAccId})");
		}

		// receiver exist
		if ((bidder != null || bidderAccId != 0)) // && !sAuctionBotConfig.IsBotChar(auction.Bidder))
		{
			MailDraft mail = new(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Won, auction),
								Global.AuctionHouseMgr.BuildAuctionWonMailBody(auction.Owner, auction.BidAmount, auction.BuyoutOrUnitPrice));

			// set owner to bidder (to prevent delete item with sender char deleting)
			// owner in `data` will set at mail receive and item extracting
			foreach (var item in auction.Items)
			{
				var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ITEM_OWNER);
				stmt.AddValue(0, auction.Bidder.Counter);
				stmt.AddValue(1, item.GUID.Counter);
				trans.Append(stmt);

				mail.AddItem(item);
			}

			if (bidder)
			{
				AuctionWonNotification packet = new();
				packet.Info.Initialize(auction, auction.Items[0]);
				bidder.SendPacket(packet);

				// FIXME: for offline player need also
				bidder.UpdateCriteria(CriteriaType.AuctionsWon, 1);
			}

			mail.SendMailTo(trans, new MailReceiver(bidder, auction.Bidder), new MailSender(this), MailCheckMask.Copied);
		}
		else
		{
			// bidder doesn't exist, delete the item
			foreach (var item in auction.Items)
				Global.AuctionHouseMgr.RemoveAItem(item.GUID, true, trans);
		}
	}

	//call this method to send mail to auction owner, when auction is successful, it does not clear ram
	public void SendAuctionSold(AuctionPosting auction, Player owner, SQLTransaction trans)
	{
		if (!owner)
			owner = Global.ObjAccessor.FindConnectedPlayer(auction.Owner);

		// owner exist
		if ((owner || Global.CharacterCacheStorage.HasCharacterCacheEntry(auction.Owner))) // && !sAuctionBotConfig.IsBotChar(auction.Owner))
		{
			var auctionHouseCut = CalculateAuctionHouseCut(auction.BidAmount);
			var profit = auction.BidAmount + auction.Deposit - auctionHouseCut;

			//FIXME: what do if owner offline
			if (owner)
			{
				owner.UpdateCriteria(CriteriaType.MoneyEarnedFromAuctions, profit);
				owner.UpdateCriteria(CriteriaType.HighestAuctionSale, auction.BidAmount);

				//send auction owner notification, bidder must be current!
				owner. //send auction owner notification, bidder must be current!
					Session.SendAuctionClosedNotification(auction, (float)WorldConfig.GetIntValue(WorldCfg.MailDeliveryDelay), true);
			}

			new MailDraft(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Sold, auction),
						Global.AuctionHouseMgr.BuildAuctionSoldMailBody(auction.Bidder, auction.BidAmount, auction.BuyoutOrUnitPrice, (uint)auction.Deposit, auctionHouseCut))
				.AddMoney(profit)
				.SendMailTo(trans, new MailReceiver(owner, auction.Owner), new MailSender(this), MailCheckMask.Copied, WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay));
		}
	}

	public void SendAuctionExpired(AuctionPosting auction, SQLTransaction trans)
	{
		var owner = Global.ObjAccessor.FindConnectedPlayer(auction.Owner);

		// owner exist
		if ((owner || Global.CharacterCacheStorage.HasCharacterCacheEntry(auction.Owner))) // && !sAuctionBotConfig.IsBotChar(auction.Owner))
		{
			if (owner)
				owner.Session.SendAuctionClosedNotification(auction, 0.0f, false);

			var itemIndex = 0;

			while (itemIndex < auction.Items.Count)
			{
				MailDraft mail = new(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Expired, auction), "");

				for (var i = 0; i < SharedConst.MaxMailItems && itemIndex < auction.Items.Count; ++i, ++itemIndex)
					mail.AddItem(auction.Items[itemIndex]);

				mail.SendMailTo(trans, new MailReceiver(owner, auction.Owner), new MailSender(this), MailCheckMask.Copied, 0);
			}
		}
		else
		{
			// owner doesn't exist, delete the item
			foreach (var item in auction.Items)
				Global.AuctionHouseMgr.RemoveAItem(item.GUID, true, trans);
		}
	}

	public void SendAuctionRemoved(AuctionPosting auction, Player owner, SQLTransaction trans)
	{
		var itemIndex = 0;

		while (itemIndex < auction.Items.Count)
		{
			MailDraft draft = new(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Cancelled, auction), "");

			for (var i = 0; i < SharedConst.MaxMailItems && itemIndex < auction.Items.Count; ++i, ++itemIndex)
				draft.AddItem(auction.Items[itemIndex]);

			draft.SendMailTo(trans, owner, new MailSender(this), MailCheckMask.Copied);
		}
	}

	//this function sends mail, when auction is cancelled to old bidder
	public void SendAuctionCancelledToBidder(AuctionPosting auction, SQLTransaction trans)
	{
		var bidder = Global.ObjAccessor.FindConnectedPlayer(auction.Bidder);

		// bidder exist
		if ((bidder || Global.CharacterCacheStorage.HasCharacterCacheEntry(auction.Bidder))) // && !sAuctionBotConfig.IsBotChar(auction.Bidder))
			new MailDraft(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Removed, auction), "")
				.AddMoney(auction.BidAmount)
				.SendMailTo(trans, new MailReceiver(bidder, auction.Bidder), new MailSender(this), MailCheckMask.Copied);
	}

	public void SendAuctionInvoice(AuctionPosting auction, Player owner, SQLTransaction trans)
	{
		if (!owner)
			owner = Global.ObjAccessor.FindConnectedPlayer(auction.Owner);

		// owner exist (online or offline)
		if ((owner || Global.CharacterCacheStorage.HasCharacterCacheEntry(auction.Owner))) // && !sAuctionBotConfig.IsBotChar(auction.Owner))
		{
			ByteBuffer tempBuffer = new();
			tempBuffer.WritePackedTime(GameTime.GetGameTime() + WorldConfig.GetIntValue(WorldCfg.MailDeliveryDelay));
			var eta = tempBuffer.ReadUInt32();

			new MailDraft(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Invoice, auction),
						Global.AuctionHouseMgr.BuildAuctionInvoiceMailBody(auction.Bidder,
																			auction.BidAmount,
																			auction.BuyoutOrUnitPrice,
																			(uint)auction.Deposit,
																			CalculateAuctionHouseCut(auction.BidAmount),
																			WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay),
																			eta))
				.SendMailTo(trans, new MailReceiver(owner, auction.Owner), new MailSender(this), MailCheckMask.Copied);
		}
	}

	class PlayerReplicateThrottleData
	{
		public uint Global;
		public uint Cursor;
		public uint Tombstone;
		public DateTime NextAllowedReplication = DateTime.MinValue;

		public bool IsReplicationInProgress()
		{
			return Cursor != Tombstone && Global != 0;
		}
	}

	class MailedItemsBatch
	{
		public readonly Item[] Items = new Item[SharedConst.MaxMailItems];
		public ulong TotalPrice;
		public uint Quantity;

		public int ItemsCount;

		public bool IsFull()
		{
			return ItemsCount >= Items.Length;
		}

		public void AddItem(Item item, ulong unitPrice)
		{
			Items[ItemsCount++] = item;
			Quantity += item.Count;
			TotalPrice += unitPrice * item.Count;
		}
	}
}