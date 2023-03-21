﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer;

public partial class WorldSession
{
	public void SendAuctionCommandResult(uint auctionId, AuctionCommand command, AuctionResult errorCode, TimeSpan delayForNextAction, InventoryResult bagError = 0)
	{
		AuctionCommandResult auctionCommandResult = new();
		auctionCommandResult.AuctionID = auctionId;
		auctionCommandResult.Command = (int)command;
		auctionCommandResult.ErrorCode = (int)errorCode;
		auctionCommandResult.BagResult = (int)bagError;
		auctionCommandResult.DesiredDelay = (uint)delayForNextAction.TotalSeconds;
		SendPacket(auctionCommandResult);
	}

	public void SendAuctionClosedNotification(AuctionPosting auction, float mailDelay, bool sold)
	{
		AuctionClosedNotification packet = new();
		packet.Info.Initialize(auction);
		packet.ProceedsMailDelay = mailDelay;
		packet.Sold = sold;
		SendPacket(packet);
	}

	public void SendAuctionOwnerBidNotification(AuctionPosting auction)
	{
		AuctionOwnerBidNotification packet = new();
		packet.Info.Initialize(auction);
		packet.Bidder = auction.Bidder;
		packet.MinIncrement = auction.CalculateMinIncrement();
		SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.AuctionListItemsByItemId)]
	void HandleAuctionListItemsByItemID(AuctionListItemsByItemID listItemsByItemID)
	{
		var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, listItemsByItemID.TaintedBy.HasValue);

		if (throttle.Throttled)
			return;

		var creature = Player.GetNPCIfCanInteractWith(listItemsByItemID.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

		if (creature == null)
		{
			Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItemsByItemID - {listItemsByItemID.Auctioneer} not found or you can't interact with him.");

			return;
		}

		// remove fake death
		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.Faction);

		AuctionListItemsResult listItemsResult = new();
		listItemsResult.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
		listItemsResult.BucketKey.ItemID = listItemsByItemID.ItemID;
		var itemTemplate = Global.ObjectMgr.GetItemTemplate(listItemsByItemID.ItemID);
		listItemsResult.ListType = itemTemplate != null && itemTemplate.MaxStackSize > 1 ? AuctionHouseListType.Commodities : AuctionHouseListType.Items;

		auctionHouse.BuildListAuctionItems(listItemsResult,
											_player,
											listItemsByItemID.ItemID,
											listItemsByItemID.Offset,
											listItemsByItemID.Sorts,
											listItemsByItemID.Sorts.Count);

		SendPacket(listItemsResult);
	}

	[WorldPacketHandler(ClientOpcodes.AuctionListOwnedItems)]
	void HandleAuctionListOwnedItems(AuctionListOwnedItems listOwnedItems)
	{
		var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, listOwnedItems.TaintedBy.HasValue);

		if (throttle.Throttled)
			return;

		var creature = Player.GetNPCIfCanInteractWith(listOwnedItems.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

		if (!creature)
		{
			Log.outDebug(LogFilter.Network, $"WORLD: HandleAuctionListOwnerItems - {listOwnedItems.Auctioneer} not found or you can't interact with him.");

			return;
		}

		// remove fake death
		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.Faction);

		AuctionListOwnedItemsResult result = new();

		auctionHouse.BuildListOwnedItems(result, _player, listOwnedItems.Offset, listOwnedItems.Sorts, listOwnedItems.Sorts.Count);
		result.DesiredDelay = (uint)throttle.DelayUntilNext.TotalSeconds;
		SendPacket(result);
	}

	[WorldPacketHandler(ClientOpcodes.AuctionRemoveItem)]
	void HandleAuctionRemoveItem(AuctionRemoveItem removeItem)
	{
		var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, removeItem.TaintedBy.HasValue, AuctionCommand.Cancel);

		if (throttle.Throttled)
			return;

		var creature = Player.GetNPCIfCanInteractWith(removeItem.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

		if (!creature)
		{
			Log.outDebug(LogFilter.Network, $"WORLD: HandleAuctionRemoveItem - {removeItem.Auctioneer} not found or you can't interact with him.");

			return;
		}

		// remove fake death
		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.Faction);

		var auction = auctionHouse.GetAuction(removeItem.AuctionID);
		var player = Player;

		SQLTransaction trans = new();

		if (auction != null && auction.Owner == player.GUID)
		{
			if (auction.Bidder.IsEmpty) // If we have a bidder, we have to send him the money he paid
			{
				var cancelCost = MathFunctions.CalculatePct(auction.BidAmount, 5u);

				if (!player.HasEnoughMoney(cancelCost)) //player doesn't have enough money
				{
					SendAuctionCommandResult(0, AuctionCommand.Cancel, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);

					return;
				}

				auctionHouse.SendAuctionCancelledToBidder(auction, trans);
				player.ModifyMoney(-(long)cancelCost);
			}

			auctionHouse.SendAuctionRemoved(auction, player, trans);
		}
		else
		{
			SendAuctionCommandResult(0, AuctionCommand.Cancel, AuctionResult.DatabaseError, throttle.DelayUntilNext);
			//this code isn't possible ... maybe there should be assert
			Log.outError(LogFilter.Network, $"CHEATER: {player.GUID} tried to cancel auction (id: {removeItem.AuctionID}) of another player or auction is null");

			return;
		}

		// client bug - instead of removing auction in the UI, it only substracts 1 from visible count
		var auctionIdForClient = auction.IsCommodity ? 0 : auction.Id;

		// Now remove the auction
		player.SaveInventoryAndGoldToDB(trans);
		auctionHouse.RemoveAuction(trans, auction);

		AddTransactionCallback(DB.Characters.AsyncCommitTransaction(trans))
			.AfterComplete(success =>
			{
				if (Player && Player.GUID == _player.GUID)
				{
					if (success)
						SendAuctionCommandResult(auctionIdForClient, AuctionCommand.Cancel, AuctionResult.Ok, throttle.DelayUntilNext); //inform player, that auction is removed
					else
						SendAuctionCommandResult(0, AuctionCommand.Cancel, AuctionResult.DatabaseError, throttle.DelayUntilNext);
				}
			});
	}

	[WorldPacketHandler(ClientOpcodes.AuctionReplicateItems)]
	void HandleReplicateItems(AuctionReplicateItems replicateItems)
	{
		var creature = Player.GetNPCIfCanInteractWith(replicateItems.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

		if (!creature)
		{
			Log.outError(LogFilter.Network, $"WORLD: HandleReplicateItems - {replicateItems.Auctioneer} not found or you can't interact with him.");

			return;
		}

		// remove fake death
		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.Faction);

		AuctionReplicateResponse response = new();

		auctionHouse.BuildReplicate(response, Player, replicateItems.ChangeNumberGlobal, replicateItems.ChangeNumberCursor, replicateItems.ChangeNumberTombstone, replicateItems.Count);

		response.DesiredDelay = WorldConfig.GetUIntValue(WorldCfg.AuctionSearchDelay) * 5;
		response.Result = 0;

		SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.AuctionRequestFavoriteList)]
	void HandleAuctionRequestFavoriteList(AuctionRequestFavoriteList requestFavoriteList)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_FAVORITE_AUCTIONS);
		stmt.AddValue(0, _player.GUID.Counter);

		QueryProcessor.AddCallback(DB.Characters.AsyncQuery(stmt))
					.WithCallback(favoriteAuctionResult =>
					{
						AuctionFavoriteList favoriteItems = new();

						if (!favoriteAuctionResult.IsEmpty())
							do
							{
								AuctionFavoriteInfo item = new();
								item.Order = favoriteAuctionResult.Read<uint>(0);
								item.ItemID = favoriteAuctionResult.Read<uint>(1);
								item.ItemLevel = favoriteAuctionResult.Read<uint>(2);
								item.BattlePetSpeciesID = favoriteAuctionResult.Read<uint>(3);
								item.SuffixItemNameDescriptionID = favoriteAuctionResult.Read<uint>(4);
								favoriteItems.Items.Add(item);
							} while (favoriteAuctionResult.NextRow());

						SendPacket(favoriteItems);
					});
	}

	[WorldPacketHandler(ClientOpcodes.AuctionSellCommodity)]
	void HandleAuctionSellCommodity(AuctionSellCommodity sellCommodity)
	{
		var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, sellCommodity.TaintedBy.HasValue, AuctionCommand.SellItem);

		if (throttle.Throttled)
			return;

		if (sellCommodity.UnitPrice == 0 || sellCommodity.UnitPrice > PlayerConst.MaxMoneyAmount)
		{
			Log.outError(LogFilter.Network, $"WORLD: HandleAuctionSellItem - Player {_player.GetName()} ({_player.GUID}) attempted to sell item with invalid price.");
			SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);

			return;
		}

		// auction house does not deal with copper
		if ((sellCommodity.UnitPrice % MoneyConstants.Silver) != 0)
		{
			SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);

			return;
		}

		var creature = Player.GetNPCIfCanInteractWith(sellCommodity.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

		if (creature == null)
		{
			Log.outError(LogFilter.Network, $"WORLD: HandleAuctionListItems - {sellCommodity.Auctioneer} not found or you can't interact with him.");

			return;
		}

		uint houseId = 0;
		var auctionHouseEntry = Global.AuctionHouseMgr.GetAuctionHouseEntry(creature.Faction, ref houseId);

		if (auctionHouseEntry == null)
		{
			Log.outError(LogFilter.Network, $"WORLD: HandleAuctionSellItem - Unit ({sellCommodity.Auctioneer}) has wrong faction.");

			return;
		}

		switch (sellCommodity.RunTime)
		{
			case 1 * SharedConst.MinAuctionTime / Time.Minute:
			case 2 * SharedConst.MinAuctionTime / Time.Minute:
			case 4 * SharedConst.MinAuctionTime / Time.Minute:
				break;
			default:
				SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.AuctionHouseBusy, throttle.DelayUntilNext);

				return;
		}

		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		// find all items for sale
		ulong totalCount = 0;
		Dictionary<ObjectGuid, (Item Item, ulong UseCount)> items2 = new();

		foreach (var itemForSale in sellCommodity.Items)
		{
			var item = _player.GetItemByGuid(itemForSale.Guid);

			if (!item)
			{
				SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);

				return;
			}

			if (item.Template.MaxStackSize == 1)
			{
				// not commodity, must use different packet
				SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);

				return;
			}

			// verify that all items belong to the same bucket
			if (!items2.Empty() && AuctionsBucketKey.ForItem(item) != AuctionsBucketKey.ForItem(items2.FirstOrDefault().Value.Item1))
			{
				SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);

				return;
			}

			if (Global.AuctionHouseMgr.GetAItem(item.GUID) ||
				!item.CanBeTraded() ||
				item.IsNotEmptyBag ||
				item.Template.HasFlag(ItemFlags.Conjured) ||
				item.ItemData.Expiration != 0 ||
				item.Count < itemForSale.UseCount)
			{
				SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);

				return;
			}

			var soldItem = items2.LookupByKey(item.GUID);
			soldItem.Item = item;
			soldItem.UseCount += itemForSale.UseCount;
			items2[item.GUID] = soldItem;

			if (item.Count < soldItem.UseCount)
			{
				// check that we have enough of this item to sell
				SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);

				return;
			}

			totalCount += itemForSale.UseCount;
		}

		if (totalCount == 0)
		{
			SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);

			return;
		}

		var auctionTime = TimeSpan.FromSeconds((long)TimeSpan.FromMinutes(sellCommodity.RunTime).TotalSeconds * WorldConfig.GetFloatValue(WorldCfg.RateAuctionTime));
		var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.Faction);

		var deposit = Global.AuctionHouseMgr.GetCommodityAuctionDeposit(items2.FirstOrDefault().Value.Item.Template, TimeSpan.FromMinutes(sellCommodity.RunTime), (uint)totalCount);

		if (!_player.HasEnoughMoney(deposit))
		{
			SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);

			return;
		}

		var auctionId = Global.ObjectMgr.GenerateAuctionID();
		AuctionPosting auction = new();
		auction.Id = auctionId;
		auction.Owner = _player.GUID;
		auction.OwnerAccount = AccountGUID;
		auction.BuyoutOrUnitPrice = sellCommodity.UnitPrice;
		auction.Deposit = deposit;
		auction.StartTime = GameTime.GetSystemTime();
		auction.EndTime = auction.StartTime + auctionTime;

		// keep track of what was cloned to undo/modify counts later
		Dictionary<Item, Item> clones = new();

		foreach (var pair in items2)
		{
			Item itemForSale;

			if (pair.Value.Item1.Count != pair.Value.Item2)
			{
				itemForSale = pair.Value.Item1.CloneItem((uint)pair.Value.Item2, _player);

				if (itemForSale == null)
				{
					Log.outError(LogFilter.Network, $"CMSG_AUCTION_SELL_COMMODITY: Could not create clone of item {pair.Value.Item1.Entry}");
					SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);

					return;
				}

				clones.Add(pair.Value.Item1, itemForSale);
			}
		}

		if (!Global.AuctionHouseMgr.PendingAuctionAdd(_player, auctionHouse.GetAuctionHouseId(), auction.Id, auction.Deposit))
		{
			SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);

			return;
		}

		/*TC_LOG_INFO("network", "CMSG_AUCTION_SELL_COMMODITY: %s %s is selling item %s %s to auctioneer %s with count " UI64FMTD " with with unit price " UI64FMTD " and with time %u (in sec) in auctionhouse %u",
			_player.GetGUID().ToString(), _player.GetName(), items2.begin().second.first.GetNameForLocaleIdx(sWorld.GetDefaultDbcLocale()),
			([&items2]()
	{
			std.stringstream ss;
			auto itr = items2.begin();
			ss << (itr++).first.ToString();
			for (; itr != items2.end(); ++itr)
				ss << ',' << itr.first.ToString();
			return ss.str();
		} ()),
	creature.GetGUID().ToString(), totalCount, sellCommodity.UnitPrice, uint32(auctionTime.count()), auctionHouse.GetAuctionHouseId());*/

		if (HasPermission(RBACPermissions.LogGmTrade))
		{
			var logItem = items2.First().Value.Item1;
			Log.outCommand(AccountId, $"GM {PlayerName} (Account: {AccountId}) create auction: {logItem.GetName(Global.WorldMgr.DefaultDbcLocale)} (Entry: {logItem.Entry} Count: {totalCount})");
		}

		SQLTransaction trans = new();

		foreach (var pair in items2)
		{
			var itemForSale = pair.Value.Item1;
			var cloneItr = clones.LookupByKey(pair.Value.Item1);

			if (cloneItr != null)
			{
				var original = itemForSale;
				original.SetCount(original.Count - (uint)pair.Value.Item2);
				original.SetState(ItemUpdateState.Changed, _player);
				_player.ItemRemovedQuestCheck(original.Entry, (uint)pair.Value.Item2);
				original.SaveToDB(trans);

				itemForSale = cloneItr;
			}
			else
			{
				_player.MoveItemFromInventory(itemForSale.BagSlot, itemForSale.Slot, true);
				itemForSale.DeleteFromInventoryDB(trans);
			}

			itemForSale.SaveToDB(trans);
			auction.Items.Add(itemForSale);
		}

		auctionHouse.AddAuction(trans, auction);
		_player.SaveInventoryAndGoldToDB(trans);

		var auctionPlayerGuid = _player.GUID;

		AddTransactionCallback(DB.Characters.AsyncCommitTransaction(trans))
			.AfterComplete(success =>
			{
				if (Player && Player.GUID == auctionPlayerGuid)
				{
					if (success)
					{
						Player.UpdateCriteria(CriteriaType.ItemsPostedAtAuction, 1);
						SendAuctionCommandResult(auctionId, AuctionCommand.SellItem, AuctionResult.Ok, throttle.DelayUntilNext);
					}
					else
					{
						SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);
					}
				}
			});
	}

	[WorldPacketHandler(ClientOpcodes.AuctionSellItem)]
	void HandleAuctionSellItem(AuctionSellItem sellItem)
	{
		var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, sellItem.TaintedBy.HasValue, AuctionCommand.SellItem);

		if (throttle.Throttled)
			return;

		if (sellItem.Items.Count != 1 || sellItem.Items[0].UseCount != 1)
		{
			SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);

			return;
		}

		if (sellItem.MinBid == 0 && sellItem.BuyoutPrice == 0)
		{
			SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);

			return;
		}

		if (sellItem.MinBid > PlayerConst.MaxMoneyAmount || sellItem.BuyoutPrice > PlayerConst.MaxMoneyAmount)
		{
			Log.outError(LogFilter.Network, $"WORLD: HandleAuctionSellItem - Player {_player.GetName()} ({_player.GUID}) attempted to sell item with higher price than max gold amount.");
			SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.Inventory, throttle.DelayUntilNext, InventoryResult.TooMuchGold);

			return;
		}

		// auction house does not deal with copper
		if ((sellItem.MinBid % MoneyConstants.Silver) != 0 || (sellItem.BuyoutPrice % MoneyConstants.Silver) != 0)
		{
			SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);

			return;
		}

		var creature = Player.GetNPCIfCanInteractWith(sellItem.Auctioneer, NPCFlags.Auctioneer, NPCFlags2.None);

		if (!creature)
		{
			Log.outError(LogFilter.Network, "WORLD: HandleAuctionSellItem - Unit (%s) not found or you can't interact with him.", sellItem.Auctioneer.ToString());

			return;
		}

		uint houseId = 0;
		var auctionHouseEntry = Global.AuctionHouseMgr.GetAuctionHouseEntry(creature.Faction, ref houseId);

		if (auctionHouseEntry == null)
		{
			Log.outError(LogFilter.Network, "WORLD: HandleAuctionSellItem - Unit (%s) has wrong faction.", sellItem.Auctioneer.ToString());

			return;
		}

		switch (sellItem.RunTime)
		{
			case 1 * SharedConst.MinAuctionTime / Time.Minute:
			case 2 * SharedConst.MinAuctionTime / Time.Minute:
			case 4 * SharedConst.MinAuctionTime / Time.Minute:
				break;
			default:
				SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.AuctionHouseBusy, throttle.DelayUntilNext);

				return;
		}

		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		var item = _player.GetItemByGuid(sellItem.Items[0].Guid);

		if (!item)
		{
			SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);

			return;
		}

		if (item.Template.MaxStackSize > 1)
		{
			// commodity, must use different packet
			SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.ItemNotFound, throttle.DelayUntilNext);

			return;
		}

		if (Global.AuctionHouseMgr.GetAItem(item.GUID) ||
			!item.CanBeTraded() ||
			item.IsNotEmptyBag ||
			item.Template.HasFlag(ItemFlags.Conjured) ||
			item.ItemData.Expiration != 0 ||
			item.Count != 1)
		{
			SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);

			return;
		}

		var auctionTime = TimeSpan.FromSeconds((long)(TimeSpan.FromMinutes(sellItem.RunTime).TotalSeconds * WorldConfig.GetFloatValue(WorldCfg.RateAuctionTime)));
		var auctionHouse = Global.AuctionHouseMgr.GetAuctionsMap(creature.Faction);

		var deposit = Global.AuctionHouseMgr.GetItemAuctionDeposit(_player, item, TimeSpan.FromMinutes(sellItem.RunTime));

		if (!_player.HasEnoughMoney(deposit))
		{
			SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);

			return;
		}

		var auctionId = Global.ObjectMgr.GenerateAuctionID();

		AuctionPosting auction = new();
		auction.Id = auctionId;
		auction.Owner = _player.GUID;
		auction.OwnerAccount = AccountGUID;
		auction.MinBid = sellItem.MinBid;
		auction.BuyoutOrUnitPrice = sellItem.BuyoutPrice;
		auction.Deposit = deposit;
		auction.BidAmount = sellItem.MinBid;
		auction.StartTime = GameTime.GetSystemTime();
		auction.EndTime = auction.StartTime + auctionTime;

		if (HasPermission(RBACPermissions.LogGmTrade))
			Log.outCommand(AccountId, $"GM {PlayerName} (Account: {AccountId}) create auction: {item.Template.GetName()} (Entry: {item.Entry} Count: {item.Count})");

		auction.Items.Add(item);

		Log.outInfo(LogFilter.Network,
					$"CMSG_AuctionAction.SellItem: {_player.GUID} {_player.GetName()} is selling item {item.GUID} {item.Template.GetName()} " +
					$"to auctioneer {creature.GUID} with count {item.Count} with initial bid {sellItem.MinBid} with buyout {sellItem.BuyoutPrice} and with time {auctionTime.TotalSeconds} " +
					$"(in sec) in auctionhouse {auctionHouse.GetAuctionHouseId()}");

		// Add to pending auctions, or fail with insufficient funds error
		if (!Global.AuctionHouseMgr.PendingAuctionAdd(_player, auctionHouse.GetAuctionHouseId(), auctionId, auction.Deposit))
		{
			SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.NotEnoughMoney, throttle.DelayUntilNext);

			return;
		}

		_player.MoveItemFromInventory(item.BagSlot, item.Slot, true);

		SQLTransaction trans = new();
		item.DeleteFromInventoryDB(trans);
		item.SaveToDB(trans);

		auctionHouse.AddAuction(trans, auction);
		_player.SaveInventoryAndGoldToDB(trans);

		var auctionPlayerGuid = _player.GUID;

		AddTransactionCallback(DB.Characters.AsyncCommitTransaction(trans))
			.AfterComplete(success =>
			{
				if (Player && Player.GUID == auctionPlayerGuid)
				{
					if (success)
					{
						Player.UpdateCriteria(CriteriaType.ItemsPostedAtAuction, 1);
						SendAuctionCommandResult(auctionId, AuctionCommand.SellItem, AuctionResult.Ok, throttle.DelayUntilNext);
					}
					else
					{
						SendAuctionCommandResult(0, AuctionCommand.SellItem, AuctionResult.DatabaseError, throttle.DelayUntilNext);
					}
				}
			});
	}

	[WorldPacketHandler(ClientOpcodes.AuctionSetFavoriteItem)]
	void HandleAuctionSetFavoriteItem(AuctionSetFavoriteItem setFavoriteItem)
	{
		var throttle = Global.AuctionHouseMgr.CheckThrottle(_player, false);

		if (throttle.Throttled)
			return;

		SQLTransaction trans = new();

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_FAVORITE_AUCTION);
		stmt.AddValue(0, _player.GUID.Counter);
		stmt.AddValue(1, setFavoriteItem.Item.Order);
		trans.Append(stmt);

		if (!setFavoriteItem.IsNotFavorite)
		{
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_FAVORITE_AUCTION);
			stmt.AddValue(0, _player.GUID.Counter);
			stmt.AddValue(1, setFavoriteItem.Item.Order);
			stmt.AddValue(2, setFavoriteItem.Item.ItemID);
			stmt.AddValue(3, setFavoriteItem.Item.ItemLevel);
			stmt.AddValue(4, setFavoriteItem.Item.BattlePetSpeciesID);
			stmt.AddValue(5, setFavoriteItem.Item.SuffixItemNameDescriptionID);
			trans.Append(stmt);
		}

		DB.Characters.CommitTransaction(trans);
	}
}