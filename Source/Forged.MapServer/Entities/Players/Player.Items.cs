// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Forged.MapServer.Chrono;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Loot;
using Forged.MapServer.Mails;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking.Packets.Equipment;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Networking.Packets.Loot;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Scripting.Interfaces.IItem;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Entities.Players;

public partial class Player
{
	public void DeleteRefundReference(ObjectGuid it)
	{
		_refundableItems.Remove(it);
	}

	public void RefundItem(Item item)
	{
		if (!item.IsRefundable)
		{
			Log.Logger.Debug("Item refund: item not refundable!");

			return;
		}

		if (item.IsRefundExpired) // item refund has expired
		{
			item.SetNotRefundable(this);
			SendItemRefundResult(item, null, 10);

			return;
		}

		if (GUID != item.RefundRecipient) // Formerly refundable item got traded
		{
			Log.Logger.Debug("Item refund: item was traded!");
			item.SetNotRefundable(this);

			return;
		}

		var iece = CliDB.ItemExtendedCostStorage.LookupByKey(item.PaidExtendedCost);

		if (iece == null)
		{
			Log.Logger.Debug("Item refund: cannot find extendedcost data.");

			return;
		}

		var store_error = false;

		for (byte i = 0; i < ItemConst.MaxItemExtCostItems; ++i)
		{
			uint count = iece.ItemCount[i];
			var itemid = iece.ItemID[i];

			if (count != 0 && itemid != 0)
			{
				List<ItemPosCount> dest = new();
				var msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemid, count);

				if (msg != InventoryResult.Ok)
				{
					store_error = true;

					break;
				}
			}
		}

		if (store_error)
		{
			SendItemRefundResult(item, iece, 10);

			return;
		}

		SendItemRefundResult(item, iece, 0);

		var moneyRefund = item.PaidMoney; // item. will be invalidated in DestroyItem

		// Save all relevant data to DB to prevent desynchronisation exploits
		SQLTransaction trans = new();

		// Delete any references to the refund data
		item.SetNotRefundable(this, true, trans, false);
		Session.CollectionMgr.RemoveTemporaryAppearance(item);

		// Destroy item
		DestroyItem(item.BagSlot, item.Slot, true);

		// Grant back extendedcost items
		for (byte i = 0; i < ItemConst.MaxItemExtCostItems; ++i)
		{
			uint count = iece.ItemCount[i];
			var itemid = iece.ItemID[i];

			if (count != 0 && itemid != 0)
			{
				List<ItemPosCount> dest = new();
				var msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemid, count);
				var it = StoreNewItem(dest, itemid, true);
				SendNewItem(it, count, true, false, true);
			}
		}

		// Grant back currencies
		for (byte i = 0; i < ItemConst.MaxItemExtCostCurrencies; ++i)
		{
			if (iece.Flags.HasAnyFlag((byte)((int)ItemExtendedCostFlags.RequireSeasonEarned1 << i)))
				continue;

			var count = iece.CurrencyCount[i];
			uint currencyid = iece.CurrencyID[i];

			if (count != 0 && currencyid != 0)
				AddCurrency(currencyid, count, CurrencyGainSource.ItemRefund);
		}

		// Grant back money
		if (moneyRefund != 0)
			ModifyMoney((long)moneyRefund); // Saved in SaveInventoryAndGoldToDB

		SaveInventoryAndGoldToDB(trans);

		DB.Characters.CommitTransaction(trans);
	}

	public void SendRefundInfo(Item item)
	{
		// This function call unsets ITEM_FLAGS_REFUNDABLE if played time is over 2 hours.
		item.UpdatePlayedTime(this);

		if (!item.IsRefundable)
		{
			Log.Logger.Debug("Item refund: item not refundable!");

			return;
		}

		if (GUID != item.RefundRecipient) // Formerly refundable item got traded
		{
			Log.Logger.Debug("Item refund: item was traded!");
			item.SetNotRefundable(this);

			return;
		}

		var iece = CliDB.ItemExtendedCostStorage.LookupByKey(item.PaidExtendedCost);

		if (iece == null)
		{
			Log.Logger.Debug("Item refund: cannot find extendedcost data.");

			return;
		}

		SetItemPurchaseData setItemPurchaseData = new()
		{
			ItemGUID = item.GUID,
			PurchaseTime = TotalPlayedTime - item.PlayedTime,
			Contents =
			{
				Money = item.PaidMoney
			}
		};

		for (byte i = 0; i < ItemConst.MaxItemExtCostItems; ++i) // item cost data
		{
			setItemPurchaseData.Contents.Items[i].ItemCount = iece.ItemCount[i];
			setItemPurchaseData.Contents.Items[i].ItemID = iece.ItemID[i];
		}

		for (byte i = 0; i < ItemConst.MaxItemExtCostCurrencies; ++i) // currency cost data
		{
			if (iece.Flags.HasAnyFlag((byte)((int)ItemExtendedCostFlags.RequireSeasonEarned1 << i)))
				continue;

			setItemPurchaseData.Contents.Currencies[i].CurrencyCount = iece.CurrencyCount[i];
			setItemPurchaseData.Contents.Currencies[i].CurrencyID = iece.CurrencyID[i];
		}

		SendPacket(setItemPurchaseData);
	}

	public void SendItemRefundResult(Item item, ItemExtendedCostRecord iece, byte error)
	{
		ItemPurchaseRefundResult itemPurchaseRefundResult = new();

		{
			ItemGUID = item.GUID,
			Result = error
		}

		i(error == 0)

		{
			itemPurchaseRefundResult.Contents = new ItemPurchaseContents();

			{
				Money = item.PaidMoney
			}

			(byte i = 0;
			i < ItemConst.MaxItemExtCostItems;
			++i) // item cost data

			{
				itemPurchaseRefundResult.Contents.Items[i].ItemCount = iece.ItemCount[i];
				itemPurchaseRefundResult.Contents.Items[i].ItemID = iece.ItemID[i];
			}

			for (byte i = 0; i < ItemConst.MaxItemExtCostCurrencies; ++i) // currency cost data
			{
				if (iece.Flags.HasAnyFlag((byte)((uint)ItemExtendedCostFlags.RequireSeasonEarned1 << i)))
					continue;

				itemPurchaseRefundResult.Contents.Currencies[i].CurrencyCount = iece.CurrencyCount[i];
				itemPurchaseRefundResult.Contents.Currencies[i].CurrencyID = iece.CurrencyID[i];
			}
		}

		SendPacket(itemPurchaseRefundResult);
	}

	public void RemoveTradeableItem(Item item)
	{
		_itemSoulboundTradeable.Remove(item.GUID);
	}

	public void SetTradeData(TradeData data)
	{
		_trade = data;
	}

	public Player GetTrader()
	{
		return _trade?.GetTrader();
	}

	public TradeData GetTradeData()
	{
		return _trade;
	}

	public void TradeCancel(bool sendback)
	{
		if (_trade != null)
		{
			var trader = _trade.GetTrader();

			// send yellow "Trade canceled" message to both traders
			if (sendback)
				Session.SendCancelTrade();

			trader.Session.SendCancelTrade();

			// cleanup
			_trade = null;
			trader._trade = null;
		}
	}

	//Durability
	public void DurabilityLossAll(double percent, bool inventory)
	{
		for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; i++)
		{
			var pItem = GetItemByPos(InventorySlots.Bag0, i);

			if (pItem != null)
				DurabilityLoss(pItem, percent);
		}

		if (inventory)
		{
			var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

			for (var i = InventorySlots.ItemStart; i < inventoryEnd; i++)
			{
				var pItem = GetItemByPos(InventorySlots.Bag0, i);

				if (pItem != null)
					DurabilityLoss(pItem, percent);
			}

			for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
			{
				var pBag = GetBagByPos(i);

				if (pBag != null)
					for (byte j = 0; j < pBag.GetBagSize(); j++)
					{
						var pItem = GetItemByPos(i, j);

						if (pItem != null)
							DurabilityLoss(pItem, percent);
					}
			}
		}
	}

	public void DurabilityLoss(Item item, double percent)
	{
		if (item == null)
			return;

		uint pMaxDurability = item.ItemData.MaxDurability;

		if (pMaxDurability == 0)
			return;

		percent /= GetTotalAuraMultiplier(AuraType.ModDurabilityLoss);

		var pDurabilityLoss = (int)(pMaxDurability * percent);

		if (pDurabilityLoss < 1)
			pDurabilityLoss = 1;

		DurabilityPointsLoss(item, pDurabilityLoss);
	}

	public void DurabilityPointsLossAll(double points, bool inventory)
	{
		for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; i++)
		{
			var pItem = GetItemByPos(InventorySlots.Bag0, i);

			if (pItem != null)
				DurabilityPointsLoss(pItem, points);
		}

		if (inventory)
		{
			var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

			for (var i = InventorySlots.ItemStart; i < inventoryEnd; i++)
			{
				var pItem = GetItemByPos(InventorySlots.Bag0, i);

				if (pItem != null)
					DurabilityPointsLoss(pItem, points);
			}

			for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
			{
				var pBag = (Bag)GetItemByPos(InventorySlots.Bag0, i);

				if (pBag != null)
					for (byte j = 0; j < pBag.GetBagSize(); j++)
					{
						var pItem = GetItemByPos(i, j);

						if (pItem != null)
							DurabilityPointsLoss(pItem, points);
					}
			}
		}
	}

	public void DurabilityPointsLoss(Item item, double points)
	{
		if (HasAuraType(AuraType.PreventDurabilityLoss))
			return;

		uint pMaxDurability = item.ItemData.MaxDurability;
		uint pOldDurability = item.ItemData.Durability;
		var pNewDurability = (int)(pOldDurability - points);

		if (pNewDurability < 0)
			pNewDurability = 0;
		else if (pNewDurability > pMaxDurability)
			pNewDurability = (int)pMaxDurability;

		if (pOldDurability != pNewDurability)
		{
			// modify item stats _before_ Durability set to 0 to pass _ApplyItemMods internal check
			if (pNewDurability == 0 && pOldDurability > 0 && item.IsEquipped)
				_ApplyItemMods(item, item.Slot, false);

			item.SetDurability((uint)pNewDurability);

			// modify item stats _after_ restore durability to pass _ApplyItemMods internal check
			if (pNewDurability > 0 && pOldDurability == 0 && item.IsEquipped)
				_ApplyItemMods(item, item.Slot, true);

			item.SetState(ItemUpdateState.Changed, this);
		}
	}

	public void DurabilityPointLossForEquipSlot(byte slot)
	{
		if (HasAuraType(AuraType.PreventDurabilityLossFromCombat))
			return;

		var pItem = GetItemByPos(InventorySlots.Bag0, slot);

		if (pItem != null)
			DurabilityPointsLoss(pItem, 1);
	}

	public void DurabilityRepairAll(bool takeCost, float discountMod, bool guildBank)
	{
		// Collecting all items that can be repaired and repair costs
		List<(Item item, ulong cost)> itemRepairCostStore = new();

		// equipped, backpack, bags itself
		var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

		for (var i = EquipmentSlot.Start; i < inventoryEnd; i++)
		{
			var item = GetItemByPos((ushort)((InventorySlots.Bag0 << 8) | i));

			if (item != null)
			{
				var cost = item.CalculateDurabilityRepairCost(discountMod);

				if (cost != 0)
					itemRepairCostStore.Add((item, cost));
			}
		}

		// items in inventory bags
		for (var j = InventorySlots.BagStart; j < InventorySlots.BagEnd; j++)
		{
			for (byte i = 0; i < ItemConst.MaxBagSize; i++)
			{
				var item = GetItemByPos((ushort)((j << 8) | i));

				if (item != null)
				{
					var cost = item.CalculateDurabilityRepairCost(discountMod);

					if (cost != 0)
						itemRepairCostStore.Add((item, cost));
				}
			}
		}

		// Handling a free repair case - just repair every item without taking cost.
		if (!takeCost)
		{
			foreach (var (item, _) in itemRepairCostStore)
				DurabilityRepair(item.Pos, false, 0.0f);

			return;
		}

		if (guildBank)
		{
			// Handling a repair for guild money case.
			// We have to repair items one by one until the guild bank has enough money available for withdrawal or until all items are repaired.

			var guild = Guild;

			if (guild == null)
				return; // silent return, client shouldn't display this button for players without guild.

			var availableGuildMoney = guild.GetMemberAvailableMoneyForRepairItems(GUID);

			if (availableGuildMoney == 0)
				return;

			// Sort the items by repair cost from lowest to highest
			itemRepairCostStore.OrderByDescending(a => a.cost);

			// We must calculate total repair cost and take money once to avoid spam in the guild bank log and reduce number of transactions in the database
			ulong totalCost = 0;

			foreach (var (item, cost) in itemRepairCostStore)
			{
				var newTotalCost = totalCost + cost;

				if (newTotalCost > availableGuildMoney || newTotalCost > PlayerConst.MaxMoneyAmount)
					break;

				totalCost = newTotalCost;
				// Repair item without taking cost. We'll do it later.
				DurabilityRepair(item.Pos, false, 0.0f);
			}

			// Take money for repairs from the guild bank
			guild.HandleMemberWithdrawMoney(Session, totalCost, true);
		}
		else
		{
			// Handling a repair for player's money case.
			// Unlike repairing for guild money, in this case we must first check if player has enough money to repair all the items at once.

			ulong totalCost = 0;

			foreach (var (_, cost) in itemRepairCostStore)
				totalCost += cost;

			if (!HasEnoughMoney(totalCost))
				return; // silent return, client should display error by itself and not send opcode.

			ModifyMoney(-(int)totalCost);

			// Payment for repair has already been taken, so just repair every item without taking cost.
			foreach (var (item, cost) in itemRepairCostStore)
				DurabilityRepair(item.Pos, false, 0.0f);
		}
	}

	public void DurabilityRepair(ushort pos, bool takeCost, float discountMod)
	{
		var item = GetItemByPos(pos);

		if (item == null)
			return;


		if (takeCost)
		{
			var cost = item.CalculateDurabilityRepairCost(discountMod);

			if (!HasEnoughMoney(cost))
			{
				Log.Logger.Debug($"Player::DurabilityRepair: Player '{GetName()}' ({GUID}) has not enough money to repair item");

				return;
			}

			ModifyMoney(-(int)cost);
		}

		var isBroken = item.IsBroken;

		item.SetDurability(item.ItemData.MaxDurability);
		item.SetState(ItemUpdateState.Changed, this);

		// reapply mods for total broken and repaired item if equipped
		if (IsEquipmentPos(pos) && isBroken)
			_ApplyItemMods(item, (byte)(pos & 255), true);
	}

	//Store Item
	public InventoryResult CanStoreItem(byte bag, byte slot, List<ItemPosCount> dest, Item pItem, bool swap = false)
	{
		if (pItem == null)
			return InventoryResult.ItemNotFound;

		return CanStoreItem(bag, slot, dest, pItem.Entry, pItem.Count, pItem, swap);
	}

	public InventoryResult CanStoreItems(List<Item> items, int count, ref uint offendingItemId)
	{
		return CanStoreItems(items.ToArray(), count, ref offendingItemId);
	}

	public InventoryResult CanStoreItems(Item[] items, int count, ref uint offendingItemId)
	{
		Item item2;

		// fill space tables, creating a mock-up of the player's inventory

		// counts
		var inventoryCounts = new uint[InventorySlots.ItemEnd - InventorySlots.ItemStart];
		var bagCounts = new uint[InventorySlots.BagEnd - InventorySlots.BagStart][];

		// Item array
		var inventoryPointers = new Item[InventorySlots.ItemEnd - InventorySlots.ItemStart];
		var bagPointers = new Item[InventorySlots.BagEnd - InventorySlots.BagStart][];

		var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

		// filling inventory
		for (var i = InventorySlots.ItemStart; i < inventoryEnd; i++)
		{
			// build items in stock backpack
			item2 = GetItemByPos(InventorySlots.Bag0, i);

			if (item2 && !item2.IsInTrade)
			{
				inventoryCounts[i - InventorySlots.ItemStart] = item2.Count;
				inventoryPointers[i - InventorySlots.ItemStart] = item2;
			}
		}

		for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
		{
			var pBag = GetBagByPos(i);

			if (pBag)
			{
				bagCounts[i - InventorySlots.BagStart] = new uint[ItemConst.MaxBagSize];
				bagPointers[i - InventorySlots.BagStart] = new Item[ItemConst.MaxBagSize];

				for (byte j = 0; j < pBag.GetBagSize(); j++)
				{
					// build item counts in equippable bags
					item2 = GetItemByPos(i, j);

					if (item2 && !item2.IsInTrade)
					{
						bagCounts[i - InventorySlots.BagStart][j] = item2.Count;
						bagPointers[i - InventorySlots.BagStart][j] = item2;
					}
				}
			}
		}

		// check free space for all items that we wish to add
		for (var k = 0; k < count; ++k)
		{
			// Incoming item
			var item = items[k];

			// no item
			if (!item)
				continue;

			var remaining_count = item.Count;

			Log.Logger.Debug($"STORAGE: CanStoreItems {k + 1}. item = {item.Entry}, count = {remaining_count}");
			var pProto = item.Template;

			// strange item
			if (pProto == null)
				return InventoryResult.ItemNotFound;

			// item used
			if (item.LootGenerated)
				return InventoryResult.LootGone;

			// item it 'bind'
			if (item.IsBindedNotWith(this))
				return InventoryResult.NotOwner;

			ItemTemplate pBagProto;

			// item is 'one item only'
			var res = CanTakeMoreSimilarItems(item, ref offendingItemId);

			if (res != InventoryResult.Ok)
				return res;

			var b_found = false;

			// search stack for merge to
			if (pProto.MaxStackSize != 1)
			{
				for (var t = InventorySlots.ItemStart; t < inventoryEnd; ++t)
				{
					item2 = inventoryPointers[t - InventorySlots.ItemStart];

					if (item2 && item2.CanBeMergedPartlyWith(pProto) == InventoryResult.Ok && inventoryCounts[t - InventorySlots.ItemStart] < pProto.MaxStackSize)
					{
						inventoryCounts[t - InventorySlots.ItemStart] += remaining_count;
						remaining_count = inventoryCounts[t - InventorySlots.ItemStart] < pProto.MaxStackSize ? 0 : inventoryCounts[t - InventorySlots.ItemStart] - pProto.MaxStackSize;

						b_found = remaining_count == 0;

						// if no pieces of the stack remain, then stop checking stock bag
						if (b_found)
							break;
					}
				}

				if (b_found)
					continue;

				for (var t = InventorySlots.BagStart; !b_found && t < InventorySlots.BagEnd; ++t)
				{
					var bag = GetBagByPos(t);

					if (bag)
					{
						if (!Item.ItemCanGoIntoBag(item.Template, bag.Template))
							continue;

						for (byte j = 0; j < bag.GetBagSize(); j++)
						{
							item2 = bagPointers[t - InventorySlots.BagStart][j];

							if (item2 && item2.CanBeMergedPartlyWith(pProto) == InventoryResult.Ok && bagCounts[t - InventorySlots.BagStart][j] < pProto.MaxStackSize)
							{
								// add count to stack so that later items in the list do not double-book
								bagCounts[t - InventorySlots.BagStart][j] += remaining_count;
								remaining_count = bagCounts[t - InventorySlots.BagStart][j] < pProto.MaxStackSize ? 0 : bagCounts[t - InventorySlots.BagStart][j] - pProto.MaxStackSize;

								b_found = remaining_count == 0;

								// if no pieces of the stack remain, then stop checking equippable bags
								if (b_found)
									break;
							}
						}
					}
				}

				if (b_found)
					continue;
			}

			b_found = false;

			// special bag case
			if (pProto.BagFamily != 0)
			{
				for (var t = InventorySlots.BagStart; !b_found && t < InventorySlots.BagEnd; ++t)
				{
					var bag = GetBagByPos(t);

					if (bag)
					{
						pBagProto = bag.Template;

						// not plain container check
						if (pBagProto != null &&
							(pBagProto.Class != ItemClass.Container || pBagProto.SubClass != (uint)ItemSubClassContainer.Container) &&
							Item.ItemCanGoIntoBag(pProto, pBagProto))
							for (uint j = 0; j < bag.GetBagSize(); j++)
								if (bagCounts[t - InventorySlots.BagStart][j] == 0)
								{
									bagCounts[t - InventorySlots.BagStart][j] = remaining_count;
									bagPointers[t - InventorySlots.BagStart][j] = item;

									b_found = true;

									break;
								}
					}
				}

				if (b_found)
					continue;
			}

			// search free slot
			b_found = false;

			for (int t = InventorySlots.ItemStart; t < inventoryEnd; ++t)
				if (inventoryCounts[t - InventorySlots.ItemStart] == 0)
				{
					inventoryCounts[t - InventorySlots.ItemStart] = 1;
					inventoryPointers[t - InventorySlots.ItemStart] = item;

					b_found = true;

					break;
				}

			if (b_found)
				continue;

			// search free slot in bags
			for (var t = InventorySlots.BagStart; !b_found && t < InventorySlots.BagEnd; ++t)
			{
				var bag = GetBagByPos(t);

				if (bag)
				{
					pBagProto = bag.Template;

					// special bag already checked
					if (pBagProto != null && (pBagProto.Class != ItemClass.Container || pBagProto.SubClass != (uint)ItemSubClassContainer.Container))
						continue;

					for (uint j = 0; j < bag.GetBagSize(); j++)
						if (bagCounts[t - InventorySlots.BagStart][j] == 0)
						{
							bagCounts[t - InventorySlots.BagStart][j] = remaining_count;
							bagPointers[t - InventorySlots.BagStart][j] = item;

							b_found = true;

							break;
						}
				}
			}

			// if no free slot found for all pieces of the item, then return an error
			if (!b_found)
				return InventoryResult.BagFull;
		}

		return InventoryResult.Ok;
	}

	public InventoryResult CanStoreNewItem(byte bag, byte slot, List<ItemPosCount> dest, uint item, uint count, out uint no_space_count)
	{
		return CanStoreItem(bag, slot, dest, item, count, null, false, out no_space_count);
	}

	public InventoryResult CanStoreNewItem(byte bag, byte slot, List<ItemPosCount> dest, uint item, uint count)
	{
		return CanStoreItem(bag, slot, dest, item, count, null, false, out _);
	}

	public Item StoreItem(List<ItemPosCount> dest, Item pItem, bool update)
	{
		if (pItem == null)
			return null;

		var lastItem = pItem;

		for (var i = 0; i < dest.Count; i++)
		{
			var itemPosCount = dest[i];
			var pos = itemPosCount.Pos;
			var count = itemPosCount.Count;

			if (i == dest.Count - 1)
			{
				lastItem = _StoreItem(pos, pItem, count, false, update);

				break;
			}

			lastItem = _StoreItem(pos, pItem, count, true, update);
		}

		AutoUnequipChildItem(lastItem);

		return lastItem;
	}

	public Item StoreNewItem(List<ItemPosCount> pos, uint itemId, bool update, uint randomBonusListId = 0, List<ObjectGuid> allowedLooters = null, ItemContext context = 0, List<uint> bonusListIDs = null, bool addToCollection = true)
	{
		uint count = 0;

		foreach (var itemPosCount in pos)
			count += itemPosCount.Count;

		var item = Item.CreateItem(itemId, count, context, this);

		if (item != null)
		{
			item.SetItemFlag(ItemFieldFlags.NewItem);

			item.SetBonuses(bonusListIDs);

			item = StoreItem(pos, item, update);

			ItemAddedQuestCheck(itemId, count);
			UpdateCriteria(CriteriaType.ObtainAnyItem, itemId, count);
			UpdateCriteria(CriteriaType.AcquireItem, itemId, count);

			item.SetFixedLevel(Level);
			item.SetItemRandomBonusList(randomBonusListId);

			if (allowedLooters is { Count: > 1 } && item.Template.MaxStackSize == 1 && item.IsSoulBound)
			{
				item.SetSoulboundTradeable(allowedLooters);
				item.SetCreatePlayedTime(TotalPlayedTime);
				AddTradeableItem(item);

				// save data
				StringBuilder ss = new();

				foreach (var guid in allowedLooters)
					ss.AppendFormat("{0} ", guid);

				var stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_BOP_TRADE);
				stmt.AddValue(0, item.GUID.Counter);
				stmt.AddValue(1, ss.ToString());
				DB.Characters.Execute(stmt);
			}

			if (addToCollection)
				Session.CollectionMgr.OnItemAdded(item);

			var childItemEntry = Global.DB2Mgr.GetItemChildEquipment(itemId);

			if (childItemEntry != null)
			{
				var childTemplate = Global.ObjectMgr.GetItemTemplate(childItemEntry.ChildItemID);

				if (childTemplate != null)
				{
					List<ItemPosCount> childDest = new();
					CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, childDest, childTemplate, ref count, false, null, ItemConst.NullBag, ItemConst.NullSlot);
					var childItem = StoreNewItem(childDest, childTemplate.Id, update, 0, null, context, null, addToCollection);

					if (childItem)
					{
						childItem.SetCreator(item.GUID);
						childItem.SetItemFlag(ItemFieldFlags.Child);
						item.SetChildItem(childItem.GUID);
					}
				}
			}

			if (item.Template.InventoryType != InventoryType.NonEquip)
				UpdateAverageItemLevelTotal();
		}

		return item;
	}

	//UseItem
	public InventoryResult CanUseItem(Item pItem, bool not_loading = true)
	{
		if (pItem != null)
		{
			Log.Logger.Debug("ItemStorage: CanUseItem item = {0}", pItem.Entry);

			if (!IsAlive && not_loading)
				return InventoryResult.PlayerDead;

			var pProto = pItem.Template;

			if (pProto != null)
			{
				if (pItem.IsBindedNotWith(this))
					return InventoryResult.NotOwner;

				if (Level < pItem.GetRequiredLevel())
					return InventoryResult.CantEquipLevelI;

				var res = CanUseItem(pProto);

				if (res != InventoryResult.Ok)
					return res;

				if (pItem.Skill != 0)
				{
					var allowEquip = false;
					var itemSkill = pItem.Skill;

					// Armor that is binded to account can "morph" from plate to mail, etc. if skill is not learned yet.
					if (pProto.Quality == ItemQuality.Heirloom && pProto.Class == ItemClass.Armor && !HasSkill(itemSkill))
						// TODO: when you right-click already equipped item it throws EQUIP_ERR_PROFICIENCY_NEEDED.
						// In fact it's a visual bug, everything works properly... I need sniffs of operations with
						// binded to account items from off server.
						switch (Class)
						{
							case PlayerClass.Hunter:
							case PlayerClass.Shaman:
								allowEquip = (itemSkill == SkillType.Mail);

								break;
							case PlayerClass.Paladin:
							case PlayerClass.Warrior:
								allowEquip = (itemSkill == SkillType.PlateMail);

								break;
						}

					if (!allowEquip && GetSkillValue(itemSkill) == 0)
						return InventoryResult.ProficiencyNeeded;
				}

				return InventoryResult.Ok;
			}
		}

		return InventoryResult.ItemNotFound;
	}

	public InventoryResult CanUseItem(ItemTemplate proto, bool skipRequiredLevelCheck = false)
	{
		// Used by group, function GroupLoot, to know if a prototype can be used by a player

		if (proto == null)
			return InventoryResult.ItemNotFound;

		if (proto.HasFlag(ItemFlags2.InternalItem))
			return InventoryResult.CantEquipEver;

		if (proto.HasFlag(ItemFlags2.FactionHorde) && Team != TeamFaction.Horde)
			return InventoryResult.CantEquipEver;

		if (proto.HasFlag(ItemFlags2.FactionAlliance) && Team != TeamFaction.Alliance)
			return InventoryResult.CantEquipEver;

		if ((proto.AllowableClass & ClassMask) == 0 || (proto.AllowableRace & (long)SharedConst.GetMaskForRace(Race)) == 0)
			return InventoryResult.CantEquipEver;

		if (proto.RequiredSkill != 0)
		{
			if (GetSkillValue((SkillType)proto.RequiredSkill) == 0)
				return InventoryResult.ProficiencyNeeded;
			else if (GetSkillValue((SkillType)proto.RequiredSkill) < proto.RequiredSkillRank)
				return InventoryResult.CantEquipSkill;
		}

		if (proto.RequiredSpell != 0 && !HasSpell(proto.RequiredSpell))
			return InventoryResult.ProficiencyNeeded;

		if (!skipRequiredLevelCheck && Level < proto.BaseRequiredLevel)
			return InventoryResult.CantEquipLevelI;

		// If World Event is not active, prevent using event dependant items
		if (proto.HolidayID != 0 && !Global.GameEventMgr.IsHolidayActive(proto.HolidayID))
			return InventoryResult.ClientLockedOut;

		if (proto.RequiredReputationFaction != 0 && (uint)GetReputationRank(proto.RequiredReputationFaction) < proto.RequiredReputationRank)
			return InventoryResult.CantEquipReputation;

		// learning (recipes, mounts, pets, etc.)
		if (proto.Effects.Count >= 2)
			if (proto.Effects[0].SpellID == 483 || proto.Effects[0].SpellID == 55884)
				if (HasSpell((uint)proto.Effects[1].SpellID))
					return InventoryResult.InternalBagError;

		var artifact = CliDB.ArtifactStorage.LookupByKey(proto.ArtifactID);

		if (artifact != null)
			if (artifact.ChrSpecializationID != GetPrimarySpecialization())
				return InventoryResult.CantUseItem;

		return InventoryResult.Ok;
	}

	public Item EquipNewItem(ushort pos, uint item, ItemContext context, bool update)
	{
		var pItem = Item.CreateItem(item, 1, context, this);

		if (pItem != null)
		{
			UpdateCriteria(CriteriaType.ObtainAnyItem, item, 1);
			var equippedItem = EquipItem(pos, pItem, update);
			ItemAddedQuestCheck(item, 1);

			return equippedItem;
		}

		return null;
	}

	public Item EquipItem(ushort pos, Item pItem, bool update)
	{
		AddEnchantmentDurations(pItem);
		AddItemDurations(pItem);

		var bag = (byte)(pos >> 8);
		var slot = (byte)(pos & 255);

		var pItem2 = GetItemByPos(bag, slot);

		if (pItem2 == null)
		{
			VisualizeItem(slot, pItem);

			if (IsAlive)
			{
				var pProto = pItem.Template;

				// item set bonuses applied only at equip and removed at unequip, and still active for broken items
				if (pProto != null && pProto.ItemSet != 0)
					Item.AddItemsSetItem(this, pItem);

				_ApplyItemMods(pItem, slot, true);

				if (pProto != null && IsInCombat && (pProto.Class == ItemClass.Weapon || pProto.InventoryType == InventoryType.Relic) && _weaponChangeTimer == 0)
				{
					var cooldownSpell = (uint)(Class == PlayerClass.Rogue ? 6123 : 6119);
					var spellProto = Global.SpellMgr.GetSpellInfo(cooldownSpell, Difficulty.None);

					if (spellProto == null)
					{
						Log.Logger.Error("Weapon switch cooldown spell {0} couldn't be found in Spell.dbc", cooldownSpell);
					}
					else
					{
						_weaponChangeTimer = spellProto.StartRecoveryTime;

						SpellHistory.AddGlobalCooldown(spellProto, TimeSpan.FromMilliseconds(_weaponChangeTimer));

						SpellCooldownPkt spellCooldown = new();

						{
							Caster = GUID,
							Flags = SpellCooldownFlags.IncludeGCD
						}

						llCooldown.SpellCooldowns.Add(new SpellCooldownStruct(cooldownSpell, 0));
						SendPacket(spellCooldown);
					}
				}
			}

			pItem.SetItemFlag2(ItemFieldFlags2.Equipped);

			if (IsInWorld && update)
			{
				pItem.AddToWorld();
				pItem.SendUpdateToPlayer(this);
			}

			ApplyEquipCooldown(pItem);

			// update expertise and armor penetration - passive auras may need it

			if (slot == EquipmentSlot.MainHand)
				UpdateExpertise(WeaponAttackType.BaseAttack);
			else if (slot == EquipmentSlot.OffHand)
				UpdateExpertise(WeaponAttackType.OffAttack);

			switch (slot)
			{
				case EquipmentSlot.MainHand:
				case EquipmentSlot.OffHand:
					RecalculateRating(CombatRating.ArmorPenetration);

					break;
			}
		}
		else
		{
			pItem2.SetCount(pItem2.Count + pItem.Count);

			if (IsInWorld && update)
				pItem2.SendUpdateToPlayer(this);

			if (IsInWorld && update)
			{
				pItem.RemoveFromWorld();
				pItem.DestroyForPlayer(this);
			}

			RemoveEnchantmentDurations(pItem);
			RemoveItemDurations(pItem);

			pItem.SetOwnerGUID(GUID); // prevent error at next SetState in case trade/mail/buy from vendor
			pItem.SetNotRefundable(this);
			pItem.ClearSoulboundTradeable(this);
			RemoveTradeableItem(pItem);
			pItem.SetState(ItemUpdateState.Removed, this);
			pItem2.SetState(ItemUpdateState.Changed, this);

			ApplyEquipCooldown(pItem2);

			return pItem2;
		}

		if (slot == EquipmentSlot.MainHand || slot == EquipmentSlot.OffHand)
			CheckTitanGripPenalty();

		// only for full equip instead adding to stack
		UpdateCriteria(CriteriaType.EquipItem, pItem.Entry);
		UpdateCriteria(CriteriaType.EquipItemInSlot, slot, pItem.Entry);

		UpdateAverageItemLevelEquipped();

		return pItem;
	}

	public void EquipChildItem(byte parentBag, byte parentSlot, Item parentItem)
	{
		var itemChildEquipment = Global.DB2Mgr.GetItemChildEquipment(parentItem.Entry);

		if (itemChildEquipment != null)
		{
			var childItem = GetChildItemByGuid(parentItem.ChildItem);

			if (childItem)
			{
				var childDest = (ushort)((InventorySlots.Bag0 << 8) | itemChildEquipment.ChildItemEquipSlot);

				if (childItem.Pos != childDest)
				{
					var dstItem = GetItemByPos(childDest);

					if (!dstItem) // empty slot, simple case
					{
						RemoveItem(childItem.BagSlot, childItem.Slot, true);
						EquipItem(childDest, childItem, true);
						AutoUnequipOffhandIfNeed();
					}
					else // have currently equipped item, not simple case
					{
						var dstbag = dstItem.BagSlot;
						var dstslot = dstItem.Slot;

						var msg = CanUnequipItem(childDest, !childItem.IsBag);

						if (msg != InventoryResult.Ok)
						{
							SendEquipError(msg, dstItem);

							return;
						}

						// check dest.src move possibility but try to store currently equipped item in the bag where the parent item is
						List<ItemPosCount> sSrc = new();
						ushort eSrc = 0;

						if (IsInventoryPos(parentBag, parentSlot))
						{
							msg = CanStoreItem(parentBag, ItemConst.NullSlot, sSrc, dstItem, true);

							if (msg != InventoryResult.Ok)
								msg = CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, sSrc, dstItem, true);
						}
						else if (IsBankPos(parentBag, parentSlot))
						{
							msg = CanBankItem(parentBag, ItemConst.NullSlot, sSrc, dstItem, true);

							if (msg != InventoryResult.Ok)
								msg = CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, sSrc, dstItem, true);
						}
						else if (IsEquipmentPos(parentBag, parentSlot))
						{
							msg = CanEquipItem(parentSlot, out eSrc, dstItem, true);

							if (msg == InventoryResult.Ok)
								msg = CanUnequipItem(eSrc, true);
						}

						if (msg != InventoryResult.Ok)
						{
							SendEquipError(msg, dstItem, childItem);

							return;
						}

						// now do moves, remove...
						RemoveItem(dstbag, dstslot, false);
						RemoveItem(childItem.BagSlot, childItem.Slot, false);

						// add to dest
						EquipItem(childDest, childItem, true);

						// add to src
						if (IsInventoryPos(parentBag, parentSlot))
							StoreItem(sSrc, dstItem, true);
						else if (IsBankPos(parentBag, parentSlot))
							BankItem(sSrc, dstItem, true);
						else if (IsEquipmentPos(parentBag, parentSlot))
							EquipItem(eSrc, dstItem, true);

						AutoUnequipOffhandIfNeed();
					}
				}
			}
		}
	}

	public void AutoUnequipChildItem(Item parentItem)
	{
		if (Global.DB2Mgr.GetItemChildEquipment(parentItem.Entry) != null)
		{
			var childItem = GetChildItemByGuid(parentItem.ChildItem);

			if (childItem)
			{
				if (IsChildEquipmentPos(childItem.Pos))
					return;

				List<ItemPosCount> dest = new();
				var count = childItem.Count;
				var result = CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, dest, childItem.Template, ref count, false, childItem, ItemConst.NullBag, ItemConst.NullSlot);

				if (result != InventoryResult.Ok)
					return;

				RemoveItem(childItem.BagSlot, childItem.Slot, true);
				StoreItem(dest, childItem, true);
			}
		}
	}

	public void SendEquipError(InventoryResult msg, Item item1 = null, Item item2 = null, uint itemId = 0)
	{
		InventoryChangeFailure failure = new();

		{
			BagResult = msg
		}

		i(msg != InventoryResult.Ok)

		{
			if (item1)
				failure.Item[0] = item1.GUID;

			if (item2)
				failure.Item[1] = item2.GUID;

			failure.ContainerBSlot = 0; // bag equip slot, used with EQUIP_ERR_EVENT_AUTOEQUIP_BIND_CONFIRM and EQUIP_ERR_ITEM_DOESNT_GO_INTO_BAG2

			switch (msg)
			{
				case InventoryResult.CantEquipLevelI:
				case InventoryResult.PurchaseLevelTooLow:
				{
					failure.Level = (item1 ? item1.GetRequiredLevel() : 0);

					break;
				}
				case InventoryResult.EventAutoequipBindConfirm: // no idea about this one...
				{
					//failure.SrcContainer
					//failure.SrcSlot
					//failure.DstContainer
					break;
				}
				case InventoryResult.ItemMaxLimitCategoryCountExceededIs:
				case InventoryResult.ItemMaxLimitCategorySocketedExceededIs:
				case InventoryResult.ItemMaxLimitCategoryEquippedExceededIs:
				{
					var proto = item1 ? item1.Template : Global.ObjectMgr.GetItemTemplate(itemId);
					failure.LimitCategory = (int)(proto != null ? proto.ItemLimitCategory : 0u);

					break;
				}
				default:
					break;
			}
		}

		SendPacket(failure);
	}

	//Add/Remove/Misc Item 
	public bool AddItem(uint itemId, uint count)
	{
		List<ItemPosCount> dest = new();
		var msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemId, count, out var noSpaceForCount);

		if (msg != InventoryResult.Ok)
			count -= noSpaceForCount;

		if (count == 0 || dest.Empty())
		{
			// @todo Send to mailbox if no space
			SendSysMessage("You don't have any space in your bags.");

			return false;
		}

		var item = StoreNewItem(dest, itemId, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(itemId));

		if (item != null)
			SendNewItem(item, count, true, false);
		else
			return false;

		return true;
	}

	public void RemoveItem(byte bag, byte slot, bool update)
	{
		// note: removeitem does not actually change the item
		// it only takes the item out of storage temporarily
		// note2: if removeitem is to be used for delinking
		// the item must be removed from the player's updatequeue

		var pItem = GetItemByPos(bag, slot);

		if (pItem != null)
		{
			Log.Logger.Debug("STORAGE: RemoveItem bag = {0}, slot = {1}, item = {2}", bag, slot, pItem.Entry);

			RemoveEnchantmentDurations(pItem);
			RemoveItemDurations(pItem);
			RemoveTradeableItem(pItem);

			if (bag == InventorySlots.Bag0)
			{
				if (slot < InventorySlots.BagEnd)
				{
					// item set bonuses applied only at equip and removed at unequip, and still active for broken items
					var pProto = pItem.Template;

					if (pProto != null && pProto.ItemSet != 0)
						Item.RemoveItemsSetItem(this, pItem);

					_ApplyItemMods(pItem, slot, false, update);

					pItem.RemoveItemFlag2(ItemFieldFlags2.Equipped);

					// remove item dependent auras and casts (only weapon and armor slots)
					if (slot < ProfessionSlots.End)
					{
						// update expertise
						if (slot == EquipmentSlot.MainHand)
						{
							// clear main hand only enchantments
							for (EnchantmentSlot enchantSlot = 0; enchantSlot < EnchantmentSlot.Max; ++enchantSlot)
							{
								var enchantment = CliDB.SpellItemEnchantmentStorage.LookupByKey(pItem.GetEnchantmentId(enchantSlot));

								if (enchantment != null && enchantment.GetFlags().HasFlag(SpellItemEnchantmentFlags.MainhandOnly))
									pItem.ClearEnchantment(enchantSlot);
							}

							UpdateExpertise(WeaponAttackType.BaseAttack);
						}
						else if (slot == EquipmentSlot.OffHand)
						{
							UpdateExpertise(WeaponAttackType.OffAttack);
						}

						// update armor penetration - passive auras may need it
						switch (slot)
						{
							case EquipmentSlot.MainHand:
							case EquipmentSlot.OffHand:
								RecalculateRating(CombatRating.ArmorPenetration);

								break;
						}
					}
				}

				_items[slot] = null;
				SetInvSlot(slot, ObjectGuid.Empty);

				if (slot < EquipmentSlot.End)
				{
					SetVisibleItemSlot(slot, null);

					if (slot == EquipmentSlot.MainHand || slot == EquipmentSlot.OffHand)
						CheckTitanGripPenalty();
				}
			}

			var pBag = GetBagByPos(bag);

			if (pBag != null)
				pBag.RemoveItem(slot, update);

			pItem.SetContainedIn(ObjectGuid.Empty);
			pItem.SetSlot(ItemConst.NullSlot);

			if (IsInWorld && update)
				pItem.SendUpdateToPlayer(this);

			AutoUnequipChildItem(pItem);

			if (bag == InventorySlots.Bag0)
				UpdateAverageItemLevelEquipped();
		}
	}

	public void AddItemWithToast(uint itemID, ushort quantity, uint bonusid)
	{
		var pItem = Item.CreateItem(itemID, quantity, ItemContext.None, this);
		pItem.AddBonuses(bonusid);
		SendDisplayToast(itemID, DisplayToastType.NewItem, false, quantity, DisplayToastMethod.PersonalLoot, 0U, pItem);
		StoreNewItemInBestSlots(itemID, quantity, ItemContext.None);
	}

	public void SendABunchOfItemsInMail(List<uint> BunchOfItems, string subject, List<uint> bonusListIDs = default)
	{
		var trans = new SQLTransaction();
		var _subject = subject;
		var draft = new MailDraft(_subject, "This is a system message. Do not answer! Don't forget to take out the items! :)");

		foreach (var item in BunchOfItems)
		{
			Log.Logger.Information("[BunchOfItems]: {}.", item);
			var pItem = Item.CreateItem(item, 1, ItemContext.None, this);

			if (pItem != null)
			{
				if (bonusListIDs != null)
					foreach (var bonus in bonusListIDs)
						pItem.AddBonuses(bonus);

				pItem.SaveToDB(trans);
				draft.AddItem(pItem);
			}
		}

		draft.SendMailTo(trans, this, new MailSender(this, MailStationery.Gm), MailCheckMask.Copied | MailCheckMask.Returned);
		DB.Characters.CommitTransaction(trans);
	}

	public void GearUpByLoadout(uint loadout_purpose, in List<uint> bonusListIDs)
	{
		// Get equipped item and store it in bag. If bag is full store it in toBeMailedCurrentEquipment to send it in mail later.
		var toBeMailedCurrentEquipment = new List<Item>();

		for (var es = EquipmentSlot.Start; es < EquipmentSlot.End; es++)
		{
			var currentEquiped = GetItemByPos(InventorySlots.Bag0, es);

			if (GetItemByPos(InventorySlots.Bag0, es))
			{
				var off_dest = new List<ItemPosCount>();

				if (CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, off_dest, currentEquiped, false) == InventoryResult.Ok)
				{
					RemoveItem(InventorySlots.Bag0, es, true);
					StoreItem(off_dest, currentEquiped, true);
				}
				else
				{
					toBeMailedCurrentEquipment.Add(currentEquiped);
				}
			}
		}

		// If there are item in the toBeMailedCurrentEquipment vector remove it from inventory and send it in mail.
		if (toBeMailedCurrentEquipment.Count > 0)
		{
			var trans = new SQLTransaction();
			var draft = new MailDraft("Inventory Full: Old Equipment.", "To equip your new gear, your old gear had to be unequiped. You did not have enough free bag space, the items that could not be added to your bag you can find in this mail.");

			foreach (var currentEquiped in toBeMailedCurrentEquipment)
			{
				MoveItemFromInventory(InventorySlots.Bag0, currentEquiped.BagSlot, true);
				Item.DeleteFromInventoryDB(trans, currentEquiped.GUID.Counter); // deletes item from character's inventory
				currentEquiped.SaveToDB(trans);                                 // recursive and not have transaction guard into self, item not in inventory and can be save standalone
				draft.AddItem(currentEquiped);
			}

			draft.SendMailTo(trans, this, new MailSender(this, MailStationery.Gm), MailCheckMask.Copied | MailCheckMask.Returned);
			DB.Characters.CommitTransaction(trans);
		}

		var toBeMailedNewItems = new List<uint>();

		// Add the new items from loadout. TODO
		//foreach (uint item in sDB2Manager.GetLowestIdItemLoadOutItemsBy(GetClass(), loadout_purpose))
		//{
		//    if (!StoreNewItemInBestSlotsWithBonus(item, 1, bonusListIDs))
		//    {
		//        toBeMailedNewItems.Add(item);
		//    }
		//}

		// If we added more item than free bag slot send the new item as well in mail.
		if (toBeMailedNewItems.Count > 0)
		{
			var trans = new SQLTransaction();
			var draft = new MailDraft("Inventory Full: New Gear.", "You did not have enough free bag space to add all your complementary new gear to your bags, those that did not fit you can find in this mail.");

			foreach (var item in toBeMailedNewItems)
			{
				var pItem = Item.CreateItem(item, 1, ItemContext.None, this);

				if (pItem != null)
				{
					foreach (var bonus in bonusListIDs)
						pItem.AddBonuses(bonus);

					pItem.SaveToDB(trans);
					draft.AddItem(pItem);
				}
			}

			draft.SendMailTo(trans, this, new MailSender(this, MailStationery.Gm), MailCheckMask.Copied | MailCheckMask.Returned);
			DB.Characters.CommitTransaction(trans);
		}

		SaveToDB();
	}

	public uint GetFreeBagSlotCount()
	{
		uint freeBagSlots = 0;

		for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
		{
			var bag = GetBagByPos(i);

			if (bag != null)
				freeBagSlots += bag.GetFreeSlots();
		}

		var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

		for (var i = InventorySlots.ItemStart; i < inventoryEnd; i++)
			if (!GetItemByPos(InventorySlots.Bag0, i))
				++freeBagSlots;

		return freeBagSlots;
	}

	public void SplitItem(ushort src, ushort dst, uint count)
	{
		var srcbag = (byte)(src >> 8);
		var srcslot = (byte)(src & 255);

		var dstbag = (byte)(dst >> 8);
		var dstslot = (byte)(dst & 255);

		var pSrcItem = GetItemByPos(srcbag, srcslot);

		if (!pSrcItem)
		{
			SendEquipError(InventoryResult.ItemNotFound, pSrcItem);

			return;
		}

		if (pSrcItem.LootGenerated) // prevent split looting item (item
		{
			//best error message found for attempting to split while looting
			SendEquipError(InventoryResult.SplitFailed, pSrcItem);

			return;
		}

		// not let split all items (can be only at cheating)
		if (pSrcItem.Count == count)
		{
			SendEquipError(InventoryResult.SplitFailed, pSrcItem);

			return;
		}

		// not let split more existed items (can be only at cheating)
		if (pSrcItem.Count < count)
		{
			SendEquipError(InventoryResult.TooFewToSplit, pSrcItem);

			return;
		}

		//! If trading
		var tradeData = GetTradeData();

		if (tradeData != null)
			//! If current item is in trade window (only possible with packet spoofing - silent return)
			if (tradeData.GetTradeSlotForItem(pSrcItem.GUID) != TradeSlots.Invalid)
				return;

		Log.Logger.Debug("STORAGE: SplitItem bag = {0}, slot = {1}, item = {2}, count = {3}", dstbag, dstslot, pSrcItem.Entry, count);
		var pNewItem = pSrcItem.CloneItem(count, this);

		if (!pNewItem)
		{
			SendEquipError(InventoryResult.ItemNotFound, pSrcItem);

			return;
		}

		if (IsInventoryPos(dst))
		{
			// change item amount before check (for unique max count check)
			pSrcItem.SetCount(pSrcItem.Count - count);

			List<ItemPosCount> dest = new();
			var msg = CanStoreItem(dstbag, dstslot, dest, pNewItem, false);

			if (msg != InventoryResult.Ok)
			{
				pSrcItem.SetCount(pSrcItem.Count + count);
				SendEquipError(msg, pSrcItem);

				return;
			}

			if (IsInWorld)
				pSrcItem.SendUpdateToPlayer(this);

			pSrcItem.SetState(ItemUpdateState.Changed, this);
			StoreItem(dest, pNewItem, true);
		}
		else if (IsBankPos(dst))
		{
			// change item amount before check (for unique max count check)
			pSrcItem.SetCount(pSrcItem.Count - count);

			List<ItemPosCount> dest = new();
			var msg = CanBankItem(dstbag, dstslot, dest, pNewItem, false);

			if (msg != InventoryResult.Ok)
			{
				pSrcItem.SetCount(pSrcItem.Count + count);
				SendEquipError(msg, pSrcItem);

				return;
			}

			if (IsInWorld)
				pSrcItem.SendUpdateToPlayer(this);

			pSrcItem.SetState(ItemUpdateState.Changed, this);
			BankItem(dest, pNewItem, true);
		}
		else if (IsEquipmentPos(dst))
		{
			// change item amount before check (for unique max count check), provide space for splitted items
			pSrcItem.SetCount(pSrcItem.Count - count);

			var msg = CanEquipItem(dstslot, out var dest, pNewItem, false);

			if (msg != InventoryResult.Ok)
			{
				pSrcItem.SetCount(pSrcItem.Count + count);
				SendEquipError(msg, pSrcItem);

				return;
			}

			if (IsInWorld)
				pSrcItem.SendUpdateToPlayer(this);

			pSrcItem.SetState(ItemUpdateState.Changed, this);
			EquipItem(dest, pNewItem, true);
			AutoUnequipOffhandIfNeed();
		}
	}

	public void SwapItem(ushort src, ushort dst)
	{
		var srcbag = (byte)(src >> 8);
		var srcslot = (byte)(src & 255);

		var dstbag = (byte)(dst >> 8);
		var dstslot = (byte)(dst & 255);

		var pSrcItem = GetItemByPos(srcbag, srcslot);
		var pDstItem = GetItemByPos(dstbag, dstslot);

		if (pSrcItem == null)
			return;

		if (pSrcItem.HasItemFlag(ItemFieldFlags.Child))
		{
			var parentItem = GetItemByGuid(pSrcItem.ItemData.Creator);

			if (parentItem)
				if (IsEquipmentPos(src))
				{
					AutoUnequipChildItem(parentItem); // we need to unequip child first since it cannot go into whatever is going to happen next
					SwapItem(dst, src);               // src is now empty
					SwapItem(parentItem.Pos, dst);    // dst is now empty

					return;
				}
		}
		else if (pDstItem && pDstItem.HasItemFlag(ItemFieldFlags.Child))
		{
			var parentItem = GetItemByGuid(pDstItem.ItemData.Creator);

			if (parentItem)
				if (IsEquipmentPos(dst))
				{
					AutoUnequipChildItem(parentItem); // we need to unequip child first since it cannot go into whatever is going to happen next
					SwapItem(src, dst);               // dst is now empty
					SwapItem(parentItem.Pos, src);    // src is now empty

					return;
				}
		}

		Log.Logger.Debug("STORAGE: SwapItem bag = {0}, slot = {1}, item = {2}", dstbag, dstslot, pSrcItem.Entry);

		if (!IsAlive)
		{
			SendEquipError(InventoryResult.PlayerDead, pSrcItem, pDstItem);

			return;
		}

		// SRC checks

		// check unequip potability for equipped items and bank bags
		if (IsEquipmentPos(src) || IsBagPos(src))
		{
			// bags can be swapped with empty bag slots, or with empty bag (items move possibility checked later)
			var msg = CanUnequipItem(src, !IsBagPos(src) || IsBagPos(dst) || (pDstItem != null && pDstItem.AsBag != null && pDstItem.AsBag.IsEmpty()));

			if (msg != InventoryResult.Ok)
			{
				SendEquipError(msg, pSrcItem, pDstItem);

				return;
			}
		}

		// prevent put equipped/bank bag in self
		if (IsBagPos(src) && srcslot == dstbag)
		{
			SendEquipError(InventoryResult.BagInBag, pSrcItem, pDstItem);

			return;
		}

		// prevent equipping bag in the same slot from its inside
		if (IsBagPos(dst) && srcbag == dstslot)
		{
			SendEquipError(InventoryResult.CantSwap, pSrcItem, pDstItem);

			return;
		}

		// DST checks
		if (pDstItem != null)
			// check unequip potability for equipped items and bank bags
			if (IsEquipmentPos(dst) || IsBagPos(dst))
			{
				// bags can be swapped with empty bag slots, or with empty bag (items move possibility checked later)
				var msg = CanUnequipItem(dst, !IsBagPos(dst) || IsBagPos(src) || (pSrcItem.AsBag != null && pSrcItem.AsBag.IsEmpty()));

				if (msg != InventoryResult.Ok)
				{
					SendEquipError(msg, pSrcItem, pDstItem);

					return;
				}
			}

		if (IsReagentBankPos(dst) && !IsReagentBankUnlocked)
		{
			SendEquipError(InventoryResult.ReagentBankLocked, pSrcItem, pDstItem);

			return;
		}

		// NOW this is or item move (swap with empty), or swap with another item (including bags in bag possitions)
		// or swap empty bag with another empty or not empty bag (with items exchange)

		// Move case
		if (pDstItem == null)
		{
			if (IsInventoryPos(dst))
			{
				List<ItemPosCount> dest = new();
				var msg = CanStoreItem(dstbag, dstslot, dest, pSrcItem, false);

				if (msg != InventoryResult.Ok)
				{
					SendEquipError(msg, pSrcItem);

					return;
				}

				RemoveItem(srcbag, srcslot, true);
				StoreItem(dest, pSrcItem, true);

				if (IsBankPos(src))
					ItemAddedQuestCheck(pSrcItem.Entry, pSrcItem.Count);
			}
			else if (IsBankPos(dst))
			{
				List<ItemPosCount> dest = new();
				var msg = CanBankItem(dstbag, dstslot, dest, pSrcItem, false);

				if (msg != InventoryResult.Ok)
				{
					SendEquipError(msg, pSrcItem);

					return;
				}

				RemoveItem(srcbag, srcslot, true);
				BankItem(dest, pSrcItem, true);

				if (!IsReagentBankPos(dst))
					ItemRemovedQuestCheck(pSrcItem.Entry, pSrcItem.Count);
			}
			else if (IsEquipmentPos(dst))
			{
				var msg = CanEquipItem(dstslot, out var _dest, pSrcItem, false);

				if (msg != InventoryResult.Ok)
				{
					SendEquipError(msg, pSrcItem);

					return;
				}

				RemoveItem(srcbag, srcslot, true);
				EquipItem(_dest, pSrcItem, true);
				AutoUnequipOffhandIfNeed();
			}

			return;
		}

		// attempt merge to / fill target item
		if (!pSrcItem.IsBag && !pDstItem.IsBag)
		{
			InventoryResult msg;
			List<ItemPosCount> sDest = new();
			ushort eDest = 0;

			if (IsInventoryPos(dst))
				msg = CanStoreItem(dstbag, dstslot, sDest, pSrcItem, false);
			else if (IsBankPos(dst))
				msg = CanBankItem(dstbag, dstslot, sDest, pSrcItem, false);
			else if (IsEquipmentPos(dst))
				msg = CanEquipItem(dstslot, out eDest, pSrcItem, false);
			else
				return;

			if (msg == InventoryResult.Ok && IsEquipmentPos(dst) && !pSrcItem.ChildItem.IsEmpty)
				msg = CanEquipChildItem(pSrcItem);

			// can be merge/fill
			if (msg == InventoryResult.Ok)
			{
				if (pSrcItem.Count + pDstItem.Count <= pSrcItem.Template.MaxStackSize)
				{
					RemoveItem(srcbag, srcslot, true);

					if (IsInventoryPos(dst))
					{
						StoreItem(sDest, pSrcItem, true);
					}
					else if (IsBankPos(dst))
					{
						BankItem(sDest, pSrcItem, true);
					}
					else if (IsEquipmentPos(dst))
					{
						EquipItem(eDest, pSrcItem, true);

						if (!pSrcItem.ChildItem.IsEmpty)
							EquipChildItem(srcbag, srcslot, pSrcItem);

						AutoUnequipOffhandIfNeed();
					}
				}
				else
				{
					pSrcItem.SetCount(pSrcItem.Count + pDstItem.Count - pSrcItem.Template.MaxStackSize);
					pDstItem.SetCount(pSrcItem.Template.MaxStackSize);
					pSrcItem.SetState(ItemUpdateState.Changed, this);
					pDstItem.SetState(ItemUpdateState.Changed, this);

					if (IsInWorld)
					{
						pSrcItem.SendUpdateToPlayer(this);
						pDstItem.SendUpdateToPlayer(this);
					}
				}

				SendRefundInfo(pDstItem);

				return;
			}
		}

		// impossible merge/fill, do real swap
		var _msg = InventoryResult.Ok;

		// check src.dest move possibility
		List<ItemPosCount> _sDest = new();
		ushort _eDest = 0;

		if (IsInventoryPos(dst))
		{
			_msg = CanStoreItem(dstbag, dstslot, _sDest, pSrcItem, true);
		}
		else if (IsBankPos(dst))
		{
			_msg = CanBankItem(dstbag, dstslot, _sDest, pSrcItem, true);
		}
		else if (IsEquipmentPos(dst))
		{
			_msg = CanEquipItem(dstslot, out _eDest, pSrcItem, true);

			if (_msg == InventoryResult.Ok)
				_msg = CanUnequipItem(_eDest, true);
		}

		if (_msg != InventoryResult.Ok)
		{
			SendEquipError(_msg, pSrcItem, pDstItem);

			return;
		}

		// check dest.src move possibility
		List<ItemPosCount> sDest2 = new();
		ushort eDest2 = 0;

		if (IsInventoryPos(src))
		{
			_msg = CanStoreItem(srcbag, srcslot, sDest2, pDstItem, true);
		}
		else if (IsBankPos(src))
		{
			_msg = CanBankItem(srcbag, srcslot, sDest2, pDstItem, true);
		}
		else if (IsEquipmentPos(src))
		{
			_msg = CanEquipItem(srcslot, out eDest2, pDstItem, true);

			if (_msg == InventoryResult.Ok)
				_msg = CanUnequipItem(eDest2, true);
		}

		if (_msg == InventoryResult.Ok && IsEquipmentPos(dst) && !pSrcItem.ChildItem.IsEmpty)
			_msg = CanEquipChildItem(pSrcItem);

		if (_msg != InventoryResult.Ok)
		{
			SendEquipError(_msg, pDstItem, pSrcItem);

			return;
		}

		// Check bag swap with item exchange (one from empty in not bag possition (equipped (not possible in fact) or store)
		var srcBag = pSrcItem.AsBag;

		if (srcBag != null)
		{
			var dstBag = pDstItem.AsBag;

			if (dstBag != null)
			{
				Bag emptyBag = null;
				Bag fullBag = null;

				if (srcBag.IsEmpty() && !IsBagPos(src))
				{
					emptyBag = srcBag;
					fullBag = dstBag;
				}
				else if (dstBag.IsEmpty() && !IsBagPos(dst))
				{
					emptyBag = dstBag;
					fullBag = srcBag;
				}

				// bag swap (with items exchange) case
				if (emptyBag != null && fullBag != null)
				{
					var emptyProto = emptyBag.Template;
					byte count = 0;

					for (byte i = 0; i < fullBag.GetBagSize(); ++i)
					{
						var bagItem = fullBag.GetItemByPos(i);

						if (bagItem == null)
							continue;

						var bagItemProto = bagItem.Template;

						if (bagItemProto == null || !Item.ItemCanGoIntoBag(bagItemProto, emptyProto))
						{
							// one from items not go to empty target bag
							SendEquipError(InventoryResult.BagInBag, pSrcItem, pDstItem);

							return;
						}

						++count;
					}

					if (count > emptyBag.GetBagSize())
					{
						// too small targeted bag
						SendEquipError(InventoryResult.CantSwap, pSrcItem, pDstItem);

						return;
					}

					// Items swap
					count = 0; // will pos in new bag

					for (byte i = 0; i < fullBag.GetBagSize(); ++i)
					{
						var bagItem = fullBag.GetItemByPos(i);

						if (bagItem == null)
							continue;

						fullBag.RemoveItem(i, true);
						emptyBag.StoreItem(count, bagItem, true);
						bagItem.SetState(ItemUpdateState.Changed, this);

						++count;
					}
				}
			}
		}

		// now do moves, remove...
		RemoveItem(dstbag, dstslot, false);
		RemoveItem(srcbag, srcslot, false);

		// add to dest
		if (IsInventoryPos(dst))
		{
			StoreItem(_sDest, pSrcItem, true);
		}
		else if (IsBankPos(dst))
		{
			BankItem(_sDest, pSrcItem, true);
		}
		else if (IsEquipmentPos(dst))
		{
			EquipItem(_eDest, pSrcItem, true);

			if (!pSrcItem.ChildItem.IsEmpty)
				EquipChildItem(srcbag, srcslot, pSrcItem);
		}

		// add to src
		if (IsInventoryPos(src))
			StoreItem(sDest2, pDstItem, true);
		else if (IsBankPos(src))
			BankItem(sDest2, pDstItem, true);
		else if (IsEquipmentPos(src))
			EquipItem(eDest2, pDstItem, true);

		// if inventory item was moved, check if we can remove dependent auras, because they were not removed in Player::RemoveItem (update was set to false)
		// do this after swaps are done, we pass nullptr because both weapons could be swapped and none of them should be ignored
		if ((srcbag == InventorySlots.Bag0 && srcslot < InventorySlots.BagEnd) || (dstbag == InventorySlots.Bag0 && dstslot < InventorySlots.BagEnd))
			ApplyItemDependentAuras(null, false);

		// if player is moving bags and is looting an item inside this bag
		// release the loot
		if (!GetAELootView().Empty())
		{
			var released = false;

			if (IsBagPos(src))
			{
				var bag = pSrcItem.AsBag;

				for (byte i = 0; i < bag.GetBagSize(); ++i)
				{
					var bagItem = bag.GetItemByPos(i);

					if (bagItem != null)
						if (GetLootByWorldObjectGUID(bagItem.GUID) != null)
						{
							Session.DoLootReleaseAll();
							released = true; // so we don't need to look at dstBag

							break;
						}
				}
			}

			if (!released && IsBagPos(dst))
			{
				var bag = pDstItem.AsBag;

				for (byte i = 0; i < bag.GetBagSize(); ++i)
				{
					var bagItem = bag.GetItemByPos(i);

					if (bagItem != null)
						if (GetLootByWorldObjectGUID(bagItem.GUID) != null)
						{
							Session.DoLootReleaseAll();

							break;
						}
				}
			}
		}

		AutoUnequipOffhandIfNeed();
	}

	public void SendNewItem(Item item, uint quantity, bool pushed, bool created, bool broadcast = false, uint dungeonEncounterId = 0)
	{
		if (item == null) // prevent crash
			return;

		ItemPushResult packet = new();

		{
			PlayerGUID = GUID,
			Slot = item.BagSlot,
			SlotInBag = item.Count == quantity ? item.Slot : -1,
			Item = new ItemInstance(item),
			//packet.QuestLogItemID;
			Quantity = quantity,
			QuantityInInventory = GetItemCount(item.Entry),
			BattlePetSpeciesID = (int)item.GetModifier(ItemModifier.BattlePetSpeciesId),
			BattlePetBreedID = (int)item.GetModifier(ItemModifier.BattlePetBreedData) & 0xFFFFFF,
			BattlePetBreedQuality = (item.GetModifier(ItemModifier.BattlePetBreedData) >> 24) & 0xFF,
			BattlePetLevel = (int)item.GetModifier(ItemModifier.BattlePetLevel),
			ItemGUID = item.GUID,
			Pushed = pushed,
			DisplayText = ItemPushResult.DisplayType.Normal,
			Created = created
		}

		p acket.IsBonusRoll;

		if (dungeonEncounterId != 0)
		{
			packet.DisplayText = ItemPushResult.DisplayType.EncounterLoot;
			packet.DungeonEncounterID = (int)dungeonEncounterId;
			packet.IsEncounterLoot = true;
		}

		if (broadcast && Group && !item.Template.HasFlag(ItemFlags3.DontReportLootLogToParty))
			Group.BroadcastPacket(packet, true);
		else
			SendPacket(packet);
	}

	public void ToggleMetaGemsActive(uint exceptslot, bool apply)
	{
		//cycle all equipped items
		for (var slot = EquipmentSlot.Start; slot < EquipmentSlot.End; ++slot)
		{
			//enchants for the slot being socketed are handled by WorldSession.HandleSocketOpcode(WorldPacket& recvData)
			if (slot == exceptslot)
				continue;

			var pItem = GetItemByPos(InventorySlots.Bag0, slot);

			if (!pItem || pItem.GetSocketColor(0) == 0) //if item has no sockets or no item is equipped go to next item
				continue;

			//cycle all (gem)enchants
			for (var enchant_slot = EnchantmentSlot.Sock1; enchant_slot < EnchantmentSlot.Sock1 + 3; ++enchant_slot)
			{
				var enchant_id = pItem.GetEnchantmentId(enchant_slot);

				if (enchant_id == 0) //if no enchant go to next enchant(slot)
					continue;

				var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);

				if (enchantEntry == null)
					continue;

				//only metagems to be (de)activated, so only enchants with condition
				uint condition = enchantEntry.ConditionID;

				if (condition != 0)
					ApplyEnchantment(pItem, enchant_slot, apply);
			}
		}
	}

	public float GetAverageItemLevel()
	{
		float sum = 0;
		uint count = 0;

		for (int i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
		{
			// don't check tabard, ranged, offhand or shirt
			if (i == EquipmentSlot.Tabard || i == EquipmentSlot.Ranged || i == EquipmentSlot.OffHand || i == EquipmentSlot.Shirt)
				continue;

			if (_items[i] != null)
				sum += _items[i].GetItemLevel(this);

			++count;
		}

		return sum / count;
	}

	public List<Item> GetCraftingReagentItemsToDeposit()
	{
		List<Item> itemList = new();

		ForEachItem(ItemSearchLocation.Inventory,
					item =>
					{
						if (item.Template.IsCraftingReagent)
							itemList.Add(item);

						return true;
					});

		return itemList;
	}

	public Item GetItemByGuid(ObjectGuid guid)
	{
		Item result = null;

		ForEachItem(ItemSearchLocation.Everywhere,
					item =>
					{
						if (item.GUID == guid)
						{
							result = item;

							return false;
						}

						return true;
					});

		return result;
	}

	public uint GetItemCount(uint item, bool inBankAlso = false, Item skipItem = null)
	{
		var countGems = skipItem != null && skipItem.Template.GemProperties != 0;

		var location = ItemSearchLocation.Equipment | ItemSearchLocation.Inventory | ItemSearchLocation.ReagentBank;

		if (inBankAlso)
			location |= ItemSearchLocation.Bank;

		uint count = 0;

		ForEachItem(location,
					pItem =>
					{
						if (pItem != skipItem)
						{
							if (pItem.Entry == item)
								count += pItem.Count;

							if (countGems)
								count += pItem.GetGemCountWithID(item);
						}

						return true;
					});

		return count;
	}

	public Item GetUseableItemByPos(byte bag, byte slot)
	{
		var item = GetItemByPos(bag, slot);

		if (!item)
			return null;

		if (!CanUseAttackType(GetAttackBySlot(slot, item.Template.InventoryType)))
			return null;

		return item;
	}

	public Item GetItemByPos(ushort pos)
	{
		var bag = (byte)(pos >> 8);
		var slot = (byte)(pos & 255);

		return GetItemByPos(bag, slot);
	}

	public Item GetItemByPos(byte bag, byte slot)
	{
		if (bag == InventorySlots.Bag0 && slot < (int)PlayerSlots.End && (slot < InventorySlots.BuyBackStart || slot >= InventorySlots.BuyBackEnd))
			return _items[slot];

		var pBag = GetBagByPos(bag);

		if (pBag != null)
			return pBag.GetItemByPos(slot);

		return null;
	}

	public Item GetItemByEntry(uint entry, ItemSearchLocation where = ItemSearchLocation.Default)
	{
		Item result = null;

		ForEachItem(where,
					item =>
					{
						if (item.Entry == entry)
						{
							result = item;

							return false;
						}

						return true;
					});

		return result;
	}

	public List<Item> GetItemListByEntry(uint entry, bool inBankAlso = false)
	{
		var location = ItemSearchLocation.Equipment | ItemSearchLocation.Inventory | ItemSearchLocation.ReagentBank;

		if (inBankAlso)
			location |= ItemSearchLocation.Bank;

		List<Item> itemList = new();

		ForEachItem(location,
					item =>
					{
						if (item.Entry == entry)
							itemList.Add(item);

						return true;
					});

		return itemList;
	}

	public bool HasItemCount(uint item, uint count = 1, bool inBankAlso = false)
	{
		var location = ItemSearchLocation.Equipment | ItemSearchLocation.Inventory | ItemSearchLocation.ReagentBank;

		if (inBankAlso)
			location |= ItemSearchLocation.Bank;

		uint currentCount = 0;

		return !ForEachItem(location,
							pItem =>
							{
								if (pItem && pItem.Entry == item && !pItem.IsInTrade)
								{
									currentCount += pItem.Count;

									if (currentCount >= count)
										return false;
								}

								return true;
							});
	}

	public static bool IsChildEquipmentPos(byte bag, byte slot)
	{
		return bag == InventorySlots.Bag0 && (slot >= InventorySlots.ChildEquipmentStart && slot < InventorySlots.ChildEquipmentEnd);
	}

	public bool IsValidPos(byte bag, byte slot, bool explicit_pos)
	{
		// post selected
		if (bag == ItemConst.NullBag && !explicit_pos)
			return true;

		if (bag == InventorySlots.Bag0)
		{
			// any post selected
			if (slot == ItemConst.NullSlot && !explicit_pos)
				return true;

			// equipment
			if (slot < EquipmentSlot.End)
				return true;

			// profession equipment
			if (slot >= ProfessionSlots.Start && slot < ProfessionSlots.End)
				return true;

			// bag equip slots
			if (slot >= InventorySlots.BagStart && slot < InventorySlots.BagEnd)
				return true;

			// reagent bag equip slots
			if (slot >= InventorySlots.ReagentBagStart && slot < InventorySlots.ReagentBagEnd)
				return true;

			// backpack slots
			if (slot >= InventorySlots.ItemStart && slot < InventorySlots.ItemStart + GetInventorySlotCount())
				return true;

			// bank main slots
			if (slot >= InventorySlots.BankItemStart && slot < InventorySlots.BankItemEnd)
				return true;

			// bank bag slots
			if (slot >= InventorySlots.BankBagStart && slot < InventorySlots.BankBagEnd)
				return true;

			// reagent bank bag slots
			if (slot >= InventorySlots.ReagentStart && slot < InventorySlots.ReagentEnd)
				return true;

			return false;
		}

		// bag content slots
		// bank bag content slots
		var pBag = GetBagByPos(bag);

		if (pBag != null)
		{
			// any post selected
			if (slot == ItemConst.NullSlot && !explicit_pos)
				return true;

			return slot < pBag.GetBagSize();
		}

		// where this?
		return false;
	}

	public Item GetChildItemByGuid(ObjectGuid guid)
	{
		Item result = null;

		ForEachItem(ItemSearchLocation.Equipment | ItemSearchLocation.Inventory,
					item =>
					{
						if (item.GUID == guid)
						{
							result = item;

							return false;
						}

						return true;
					});

		return result;
	}

	public byte GetItemLimitCategoryQuantity(ItemLimitCategoryRecord limitEntry)
	{
		var limit = limitEntry.Quantity;

		var limitConditions = Global.DB2Mgr.GetItemLimitCategoryConditions(limitEntry.Id);

		foreach (var limitCondition in limitConditions)
		{
			var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(limitCondition.PlayerConditionID);

			if (playerCondition == null || ConditionManager.IsPlayerMeetingCondition(this, playerCondition))
				limit += (byte)limitCondition.AddQuantity;
		}

		return limit;
	}

	public void DestroyConjuredItems(bool update)
	{
		// used when entering arena
		// destroys all conjured items
		Log.Logger.Debug("STORAGE: DestroyConjuredItems");

		// in inventory
		var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

		for (var i = InventorySlots.ItemStart; i < inventoryEnd; i++)
		{
			var pItem = GetItemByPos(InventorySlots.Bag0, i);

			if (pItem)
				if (pItem.IsConjuredConsumable)
					DestroyItem(InventorySlots.Bag0, i, update);
		}

		// in inventory bags
		for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
		{
			var pBag = GetBagByPos(i);

			if (pBag)
				for (byte j = 0; j < pBag.GetBagSize(); j++)
				{
					var pItem = pBag.GetItemByPos(j);

					if (pItem)
						if (pItem.IsConjuredConsumable)
							DestroyItem(i, j, update);
				}
		}

		// in equipment and bag list
		for (var i = EquipmentSlot.Start; i < InventorySlots.BagEnd; i++)
		{
			var pItem = GetItemByPos(InventorySlots.Bag0, i);

			if (pItem)
				if (pItem.IsConjuredConsumable)
					DestroyItem(InventorySlots.Bag0, i, update);
		}
	}

	public InventoryResult CanRollNeedForItem(ItemTemplate proto, Map map, bool restrictOnlyLfg)
	{
		if (restrictOnlyLfg)
		{
			if (!Group || !Group.IsLFGGroup)
				return InventoryResult.Ok; // not in LFG group

			// check if looted object is inside the lfg dungeon
			if (!Global.LFGMgr.InLfgDungeonMap(Group.GUID, map.Id, map.DifficultyID))
				return InventoryResult.Ok;
		}

		if (proto == null)
			return InventoryResult.ItemNotFound;

		// Used by group, function GroupLoot, to know if a prototype can be used by a player
		if ((proto.AllowableClass & ClassMask) == 0 || (proto.AllowableRace & (long)SharedConst.GetMaskForRace(Race)) == 0)
			return InventoryResult.CantEquipEver;

		if (proto.RequiredSpell != 0 && !HasSpell(proto.RequiredSpell))
			return InventoryResult.ProficiencyNeeded;

		if (proto.RequiredSkill != 0)
		{
			if (GetSkillValue((SkillType)proto.RequiredSkill) == 0)
				return InventoryResult.ProficiencyNeeded;
			else if (GetSkillValue((SkillType)proto.RequiredSkill) < proto.RequiredSkillRank)
				return InventoryResult.CantEquipSkill;
		}

		if (proto.Class == ItemClass.Weapon && GetSkillValue(proto.GetSkill()) == 0)
			return InventoryResult.ProficiencyNeeded;

		if (proto.Class == ItemClass.Armor && proto.InventoryType != InventoryType.Cloak)
		{
			var classesEntry = CliDB.ChrClassesStorage.LookupByKey(Class);

			if ((classesEntry.ArmorTypeMask & 1 << (int)proto.SubClass) == 0)
				return InventoryResult.ClientLockedOut;
		}

		return InventoryResult.Ok;
	}

	public void AddItemToBuyBackSlot(Item pItem)
	{
		if (pItem != null)
		{
			var slot = _currentBuybackSlot;

			// if current back slot non-empty search oldest or free
			if (_items[slot] != null)
			{
				var oldest_time = ActivePlayerData.BuybackTimestamp[0];
				uint oldest_slot = InventorySlots.BuyBackStart;

				for (byte i = InventorySlots.BuyBackStart + 1; i < InventorySlots.BuyBackEnd; ++i)
				{
					// found empty
					if (!_items[i])
					{
						oldest_slot = i;

						break;
					}

					var i_time = ActivePlayerData.BuybackTimestamp[i - InventorySlots.BuyBackStart];

					if (oldest_time > i_time)
					{
						oldest_time = i_time;
						oldest_slot = i;
					}
				}

				// find oldest
				slot = oldest_slot;
			}

			RemoveItemFromBuyBackSlot(slot, true);
			Log.Logger.Debug("STORAGE: AddItemToBuyBackSlot item = {0}, slot = {1}", pItem.Entry, slot);

			_items[slot] = pItem;
			var time = GameTime.GetGameTime();
			var etime = (uint)(time - _logintime + (30 * 3600));
			var eslot = slot - InventorySlots.BuyBackStart;

			SetInvSlot(slot, pItem.GUID);
			var proto = pItem.Template;

			if (proto != null)
				SetBuybackPrice(eslot, proto.SellPrice * pItem.Count);
			else
				SetBuybackPrice(eslot, 0);

			SetBuybackTimestamp(eslot, etime);

			// move to next (for non filled list is move most optimized choice)
			if (_currentBuybackSlot < InventorySlots.BuyBackEnd - 1)
				++_currentBuybackSlot;
		}
	}

	public bool BuyCurrencyFromVendorSlot(ObjectGuid vendorGuid, uint vendorSlot, uint currency, uint count)
	{
		// cheating attempt
		if (count < 1)
			count = 1;

		if (!IsAlive)
			return false;

		var proto = CliDB.CurrencyTypesStorage.LookupByKey(currency);

		if (proto == null)
		{
			SendBuyError(BuyResult.CantFindItem, null, currency);

			return false;
		}

		var creature = GetNPCIfCanInteractWith(vendorGuid, NPCFlags.Vendor, NPCFlags2.None);

		if (!creature)
		{
			Log.Logger.Debug("WORLD: BuyCurrencyFromVendorSlot - {0} not found or you can't interact with him.", vendorGuid.ToString());
			SendBuyError(BuyResult.DistanceTooFar, null, currency);

			return false;
		}

		var vItems = creature.VendorItems;

		if (vItems == null || vItems.Empty())
		{
			SendBuyError(BuyResult.CantFindItem, creature, currency);

			return false;
		}

		if (vendorSlot >= vItems.GetItemCount())
		{
			SendBuyError(BuyResult.CantFindItem, creature, currency);

			return false;
		}

		var crItem = vItems.GetItem(vendorSlot);

		// store diff item (cheating)
		if (crItem == null || crItem.Item != currency || crItem.Type != ItemVendorType.Currency)
		{
			SendBuyError(BuyResult.CantFindItem, creature, currency);

			return false;
		}

		if ((count % crItem.Maxcount) != 0)
		{
			SendEquipError(InventoryResult.CantBuyQuantity);

			return false;
		}

		var stacks = count / crItem.Maxcount;
		ItemExtendedCostRecord iece;

		if (crItem.ExtendedCost != 0)
		{
			iece = CliDB.ItemExtendedCostStorage.LookupByKey(crItem.ExtendedCost);

			if (iece == null)
			{
				Log.Logger.Error("Currency {0} have wrong ExtendedCost field value {1}", currency, crItem.ExtendedCost);

				return false;
			}

			for (byte i = 0; i < ItemConst.MaxItemExtCostItems; ++i)
				if (iece.ItemID[i] != 0 && !HasItemCount(iece.ItemID[i], (iece.ItemCount[i] * stacks)))
				{
					SendEquipError(InventoryResult.VendorMissingTurnins);

					return false;
				}

			for (byte i = 0; i < ItemConst.MaxItemExtCostCurrencies; ++i)
			{
				if (iece.CurrencyID[i] == 0)
					continue;

				var entry = CliDB.CurrencyTypesStorage.LookupByKey(iece.CurrencyID[i]);

				if (entry == null)
				{
					SendBuyError(BuyResult.CantFindItem, creature, currency); // Find correct error

					return false;
				}

				if (iece.Flags.HasAnyFlag((byte)((uint)ItemExtendedCostFlags.RequireSeasonEarned1 << i)))
				{
					// Not implemented
					SendEquipError(InventoryResult.VendorMissingTurnins); // Find correct error

					return false;
				}
				else if (!HasCurrency(iece.CurrencyID[i], (iece.CurrencyCount[i] * stacks)))
				{
					SendEquipError(InventoryResult.VendorMissingTurnins); // Find correct error

					return false;
				}
			}

			// check for personal arena rating requirement
			if (GetMaxPersonalArenaRatingRequirement(iece.ArenaBracket) < iece.RequiredArenaRating)
			{
				// probably not the proper equip err
				SendEquipError(InventoryResult.CantEquipRank);

				return false;
			}

			if (iece.MinFactionID != 0 && (uint)GetReputationRank(iece.MinFactionID) < iece.RequiredAchievement)
			{
				SendBuyError(BuyResult.ReputationRequire, creature, currency);

				return false;
			}

			if (iece.Flags.HasAnyFlag((byte)ItemExtendedCostFlags.RequireGuild) && GuildId == 0)
			{
				SendEquipError(InventoryResult.VendorMissingTurnins); // Find correct error

				return false;
			}

			if (iece.RequiredAchievement != 0 && !HasAchieved(iece.RequiredAchievement))
			{
				SendEquipError(InventoryResult.VendorMissingTurnins); // Find correct error

				return false;
			}
		}
		else // currencies have no price defined, can only be bought with ExtendedCost
		{
			SendBuyError(BuyResult.CantFindItem, null, currency);

			return false;
		}

		AddCurrency(currency, count, CurrencyGainSource.Vendor);

		if (iece != null)
		{
			for (byte i = 0; i < ItemConst.MaxItemExtCostItems; ++i)
			{
				if (iece.ItemID[i] == 0)
					continue;

				DestroyItemCount(iece.ItemID[i], iece.ItemCount[i] * stacks, true);
			}

			for (byte i = 0; i < ItemConst.MaxItemExtCostCurrencies; ++i)
			{
				if (iece.CurrencyID[i] == 0)
					continue;

				if (iece.Flags.HasAnyFlag((byte)((uint)ItemExtendedCostFlags.RequireSeasonEarned1 << i)))
					continue;

				RemoveCurrency(iece.CurrencyID[i], (int)(iece.CurrencyCount[i] * stacks), CurrencyDestroyReason.Vendor);
			}
		}

		return true;
	}

	public bool BuyItemFromVendorSlot(ObjectGuid vendorguid, uint vendorslot, uint item, byte count, byte bag, byte slot)
	{
		// cheating attempt
		if (count < 1)
			count = 1;

		// cheating attempt
		if (slot > ItemConst.MaxBagSize && slot != ItemConst.NullSlot)
			return false;

		if (!IsAlive)
			return false;

		var pProto = Global.ObjectMgr.GetItemTemplate(item);

		if (pProto == null)
		{
			SendBuyError(BuyResult.CantFindItem, null, item);

			return false;
		}

		if (!Convert.ToBoolean(pProto.AllowableClass & ClassMask) && pProto.Bonding == ItemBondingType.OnAcquire && !IsGameMaster)
		{
			SendBuyError(BuyResult.CantFindItem, null, item);

			return false;
		}

		if (!IsGameMaster && ((pProto.HasFlag(ItemFlags2.FactionHorde) && Team == TeamFaction.Alliance) || (pProto.HasFlag(ItemFlags2.FactionAlliance) && Team == TeamFaction.Horde)))
			return false;

		var creature = GetNPCIfCanInteractWith(vendorguid, NPCFlags.Vendor, NPCFlags2.None);

		if (!creature)
		{
			Log.Logger.Debug("WORLD: BuyItemFromVendor - {0} not found or you can't interact with him.", vendorguid.ToString());
			SendBuyError(BuyResult.DistanceTooFar, null, item);

			return false;
		}

		if (!Global.ConditionMgr.IsObjectMeetingVendorItemConditions(creature.Entry, item, this, creature))
		{
			Log.Logger.Debug("BuyItemFromVendor: conditions not met for creature entry {0} item {1}", creature.Entry, item);
			SendBuyError(BuyResult.CantFindItem, creature, item);

			return false;
		}

		var vItems = creature.VendorItems;

		if (vItems == null || vItems.Empty())
		{
			SendBuyError(BuyResult.CantFindItem, creature, item);

			return false;
		}

		if (vendorslot >= vItems.GetItemCount())
		{
			SendBuyError(BuyResult.CantFindItem, creature, item);

			return false;
		}

		var crItem = vItems.GetItem(vendorslot);

		// store diff item (cheating)
		if (crItem == null || crItem.Item != item)
		{
			SendBuyError(BuyResult.CantFindItem, creature, item);

			return false;
		}

		var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(crItem.PlayerConditionId);

		if (playerCondition != null)
			if (!ConditionManager.IsPlayerMeetingCondition(this, playerCondition))
			{
				SendEquipError(InventoryResult.ItemLocked);

				return false;
			}

		// check current item amount if it limited
		if (crItem.Maxcount != 0)
			if (creature.GetVendorItemCurrentCount(crItem) < count)
			{
				SendBuyError(BuyResult.ItemAlreadySold, creature, item);

				return false;
			}

		if (pProto.RequiredReputationFaction != 0 && ((uint)GetReputationRank(pProto.RequiredReputationFaction) < pProto.RequiredReputationRank))
		{
			SendBuyError(BuyResult.ReputationRequire, creature, item);

			return false;
		}

		if (crItem.ExtendedCost != 0)
		{
			// Can only buy full stacks for extended cost
			if ((count % pProto.BuyCount) != 0)
			{
				SendEquipError(InventoryResult.CantBuyQuantity);

				return false;
			}

			var stacks = count / pProto.BuyCount;
			var iece = CliDB.ItemExtendedCostStorage.LookupByKey(crItem.ExtendedCost);

			if (iece == null)
			{
				Log.Logger.Error("Item {0} have wrong ExtendedCost field value {1}", pProto.Id, crItem.ExtendedCost);

				return false;
			}

			for (byte i = 0; i < ItemConst.MaxItemExtCostItems; ++i)
				if (iece.ItemID[i] != 0 && !HasItemCount(iece.ItemID[i], iece.ItemCount[i] * stacks))
				{
					SendEquipError(InventoryResult.VendorMissingTurnins);

					return false;
				}

			for (byte i = 0; i < ItemConst.MaxItemExtCostCurrencies; ++i)
			{
				if (iece.CurrencyID[i] == 0)
					continue;

				var entry = CliDB.CurrencyTypesStorage.LookupByKey(iece.CurrencyID[i]);

				if (entry == null)
				{
					SendBuyError(BuyResult.CantFindItem, creature, item);

					return false;
				}

				if (iece.Flags.HasAnyFlag((byte)((uint)ItemExtendedCostFlags.RequireSeasonEarned1 << i)))
				{
					SendEquipError(InventoryResult.VendorMissingTurnins); // Find correct error

					return false;
				}
				else if (!HasCurrency(iece.CurrencyID[i], iece.CurrencyCount[i] * stacks))
				{
					SendEquipError(InventoryResult.VendorMissingTurnins);

					return false;
				}
			}

			// check for personal arena rating requirement
			if (GetMaxPersonalArenaRatingRequirement(iece.ArenaBracket) < iece.RequiredArenaRating)
			{
				// probably not the proper equip err
				SendEquipError(InventoryResult.CantEquipRank);

				return false;
			}

			if (iece.MinFactionID != 0 && (uint)GetReputationRank(iece.MinFactionID) < iece.MinReputation)
			{
				SendBuyError(BuyResult.ReputationRequire, creature, item);

				return false;
			}

			if (iece.Flags.HasAnyFlag((byte)ItemExtendedCostFlags.RequireGuild) && GuildId == 0)
			{
				SendEquipError(InventoryResult.VendorMissingTurnins); // Find correct error

				return false;
			}

			if (iece.RequiredAchievement != 0 && !HasAchieved(iece.RequiredAchievement))
			{
				SendEquipError(InventoryResult.VendorMissingTurnins); // Find correct error

				return false;
			}
		}

		ulong price = 0;

		if (pProto.BuyPrice > 0) //Assume price cannot be negative (do not know why it is int32)
		{
			var buyPricePerItem = (float)pProto.BuyPrice / pProto.BuyCount;
			var maxCount = (ulong)(PlayerConst.MaxMoneyAmount / buyPricePerItem);

			if (count > maxCount)
			{
				Log.Logger.Error("Player {0} tried to buy {1} item id {2}, causing overflow", GetName(), count, pProto.Id);
				count = (byte)maxCount;
			}

			price = (ulong)(buyPricePerItem * count); //it should not exceed MAX_MONEY_AMOUNT

			// reputation discount
			price = (ulong)Math.Floor(price * GetReputationPriceDiscount(creature));
			price = pProto.BuyPrice > 0 ? Math.Max(1ul, price) : price;

			var priceMod = GetTotalAuraModifier(AuraType.ModVendorItemsPrices);

			if (priceMod != 0)
				price -= (ulong)MathFunctions.CalculatePct(price, priceMod);

			if (!HasEnoughMoney(price))
			{
				SendBuyError(BuyResult.NotEnoughtMoney, creature, item);

				return false;
			}
		}

		if ((bag == ItemConst.NullBag && slot == ItemConst.NullSlot) || IsInventoryPos(bag, slot))
		{
			if (!_StoreOrEquipNewItem(vendorslot, item, count, bag, slot, (int)price, pProto, creature, crItem, true))
				return false;
		}
		else if (IsEquipmentPos(bag, slot))
		{
			if (count != 1)
			{
				SendEquipError(InventoryResult.NotEquippable);

				return false;
			}

			if (!_StoreOrEquipNewItem(vendorslot, item, count, bag, slot, (int)price, pProto, creature, crItem, false))
				return false;
		}
		else
		{
			SendEquipError(InventoryResult.WrongSlot);

			return false;
		}

		if (crItem.Maxcount != 0) // bought
		{
			if (pProto.Quality > ItemQuality.Epic || (pProto.Quality == ItemQuality.Epic && pProto.BaseItemLevel >= GuildConst.MinNewsItemLevel))
			{
				var guild = Guild;

				if (guild != null)
					guild.AddGuildNews(GuildNews.ItemPurchased, GUID, 0, item);
			}

			UpdateCriteria(CriteriaType.BuyItemsFromVendors, 1);

			return true;
		}

		return false;
	}

	public uint GetMaxPersonalArenaRatingRequirement(uint minarenaslot)
	{
		// returns the maximal personal arena rating that can be used to purchase items requiring this condition
		// so return max[in arenateams](personalrating[teamtype])
		uint max_personal_rating = 0;

		for (var i = (byte)minarenaslot; i < SharedConst.MaxArenaSlot; ++i)
		{
			var p_rating = GetArenaPersonalRating(i);

			if (max_personal_rating < p_rating)
				max_personal_rating = p_rating;
		}

		return max_personal_rating;
	}

	public void SendItemRetrievalMail(uint itemEntry, uint count, ItemContext context)
	{
		MailSender sender = new(MailMessageType.Creature, 34337);
		MailDraft draft = new("Recovered Item", "We recovered a lost item in the twisting nether and noted that it was yours.$B$BPlease find said object enclosed."); // This is the text used in Cataclysm, it probably wasn't changed.
		SQLTransaction trans = new();

		var item = Item.CreateItem(itemEntry, count, context, null);

		if (item)
		{
			item.SaveToDB(trans);
			draft.AddItem(item);
		}

		draft.SendMailTo(trans, new MailReceiver(this, GUID.Counter), sender);
		DB.Characters.CommitTransaction(trans);
	}

	public void SetBuybackPrice(uint slot, uint price)
	{
		SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.BuybackPrice, (int)slot), price);
	}

	public void SetBuybackTimestamp(uint slot, ulong timestamp)
	{
		SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.BuybackTimestamp, (int)slot), timestamp);
	}

	public Item GetItemFromBuyBackSlot(uint slot)
	{
		Log.Logger.Debug("STORAGE: GetItemFromBuyBackSlot slot = {0}", slot);

		if (slot >= InventorySlots.BuyBackStart && slot < InventorySlots.BuyBackEnd)
			return _items[slot];

		return null;
	}

	public void RemoveItemFromBuyBackSlot(uint slot, bool del)
	{
		Log.Logger.Debug("STORAGE: RemoveItemFromBuyBackSlot slot = {0}", slot);

		if (slot >= InventorySlots.BuyBackStart && slot < InventorySlots.BuyBackEnd)
		{
			var pItem = _items[slot];

			if (pItem)
			{
				pItem.RemoveFromWorld();

				if (del)
				{
					var itemTemplate = pItem.Template;

					if (itemTemplate != null)
						if (itemTemplate.HasFlag(ItemFlags.HasLoot))
							Global.LootItemStorage.RemoveStoredLootForContainer(pItem.GUID.Counter);

					pItem.SetState(ItemUpdateState.Removed, this);
				}
			}

			_items[slot] = null;

			var eslot = slot - InventorySlots.BuyBackStart;
			SetInvSlot(slot, ObjectGuid.Empty);
			SetBuybackPrice(eslot, 0);
			SetBuybackTimestamp(eslot, 0);

			// if current backslot is filled set to now free slot
			if (_items[_currentBuybackSlot])
				_currentBuybackSlot = slot;
		}
	}

	public bool HasItemTotemCategory(uint TotemCategory)
	{
		foreach (var providedTotemCategory in GetAuraEffectsByType(AuraType.ProvideTotemCategory))
			if (Global.DB2Mgr.IsTotemCategoryCompatibleWith((uint)providedTotemCategory.MiscValueB, TotemCategory))
				return true;

		var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

		for (var i = EquipmentSlot.Start; i < inventoryEnd; ++i)
		{
			var item = GetUseableItemByPos(InventorySlots.Bag0, i);

			if (item && Global.DB2Mgr.IsTotemCategoryCompatibleWith(item.Template.TotemCategory, TotemCategory))
				return true;
		}

		for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
		{
			var bag = GetBagByPos(i);

			if (bag)
				for (byte j = 0; j < bag.GetBagSize(); ++j)
				{
					var item = GetUseableItemByPos(i, j);

					if (item && Global.DB2Mgr.IsTotemCategoryCompatibleWith(item.Template.TotemCategory, TotemCategory))
						return true;
				}
		}

		for (var i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
		{
			var item = GetUseableItemByPos(InventorySlots.Bag0, i);

			if (item && Global.DB2Mgr.IsTotemCategoryCompatibleWith(item.Template.TotemCategory, TotemCategory))
				return true;
		}

		return false;
	}

	public void _ApplyItemMods(Item item, byte slot, bool apply, bool updateItemAuras = true)
	{
		if (slot >= InventorySlots.BagEnd || item == null)
			return;

		var proto = item.Template;

		if (proto == null)
			return;

		// not apply/remove mods for broken item
		if (item.IsBroken)
			return;

		Log.Logger.Information("applying mods for item {0} ", item.GUID.ToString());

		if (item.GetSocketColor(0) != 0) //only (un)equipping of items with sockets can influence metagems, so no need to waste time with normal items
			CorrectMetaGemEnchants(slot, apply);

		_ApplyItemBonuses(item, slot, apply);
		ApplyItemEquipSpell(item, apply);

		if (updateItemAuras)
		{
			ApplyItemDependentAuras(item, apply);
			var attackType = GetAttackBySlot(slot, item.Template.InventoryType);

			if (attackType != WeaponAttackType.Max)
				UpdateWeaponDependentAuras(attackType);
		}

		ApplyArtifactPowers(item, apply);
		ApplyAzeritePowers(item, apply);
		ApplyEnchantment(item, apply);

		Log.Logger.Debug("_ApplyItemMods complete.");
	}

	public void _ApplyItemBonuses(Item item, byte slot, bool apply)
	{
		var proto = item.Template;

		if (slot >= InventorySlots.BagEnd || proto == null)
			return;

		var itemLevel = item.GetItemLevel(this);
		var combatRatingMultiplier = 1.0f;
		var ratingMult = CliDB.CombatRatingsMultByILvlGameTable.GetRow(itemLevel);

		if (ratingMult != null)
			combatRatingMultiplier = CliDB.GetIlvlStatMultiplier(ratingMult, proto.InventoryType);

		for (byte i = 0; i < ItemConst.MaxStats; ++i)
		{
			var statType = item.GetItemStatType(i);

			if (statType == -1)
				continue;

			var val = item.GetItemStatValue(i, this);

			if (val == 0)
				continue;

			switch ((ItemModType)statType)
			{
				case ItemModType.Mana:
					HandleStatFlatModifier(UnitMods.Mana, UnitModifierFlatType.Base, (float)val, apply);

					break;
				case ItemModType.Health: // modify HP
					HandleStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Base, (float)val, apply);

					break;
				case ItemModType.Agility: // modify agility
					HandleStatFlatModifier(UnitMods.StatAgility, UnitModifierFlatType.Base, (float)val, apply);
					UpdateStatBuffMod(Stats.Agility);

					break;
				case ItemModType.Strength: //modify strength
					HandleStatFlatModifier(UnitMods.StatStrength, UnitModifierFlatType.Base, (float)val, apply);
					UpdateStatBuffMod(Stats.Strength);

					break;
				case ItemModType.Intellect: //modify intellect
					HandleStatFlatModifier(UnitMods.StatIntellect, UnitModifierFlatType.Base, (float)val, apply);
					UpdateStatBuffMod(Stats.Intellect);

					break;
				//case ItemModType.Spirit:                           //modify spirit
				//HandleStatModifier(UnitMods.StatSpirit, UnitModifierType.BaseValue, (float)val, apply);
				//ApplyStatBuffMod(Stats.Spirit, MathFunctions.CalculatePct(val, GetModifierValue(UnitMods.StatSpirit, UnitModifierType.BasePCTExcludeCreate)), apply);
				//break;
				case ItemModType.Stamina: //modify stamina
					var staminaMult = CliDB.StaminaMultByILvlGameTable.GetRow(itemLevel);

					if (staminaMult != null)
						val = (int)(val * CliDB.GetIlvlStatMultiplier(staminaMult, proto.InventoryType));

					HandleStatFlatModifier(UnitMods.StatStamina, UnitModifierFlatType.Base, (float)val, apply);
					UpdateStatBuffMod(Stats.Stamina);

					break;
				case ItemModType.DefenseSkillRating:
					ApplyRatingMod(CombatRating.DefenseSkill, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.DodgeRating:
					ApplyRatingMod(CombatRating.Dodge, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.ParryRating:
					ApplyRatingMod(CombatRating.Parry, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.BlockRating:
					ApplyRatingMod(CombatRating.Block, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.HitMeleeRating:
					ApplyRatingMod(CombatRating.HitMelee, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.HitRangedRating:
					ApplyRatingMod(CombatRating.HitRanged, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.HitSpellRating:
					ApplyRatingMod(CombatRating.HitSpell, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.CritMeleeRating:
					ApplyRatingMod(CombatRating.CritMelee, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.CritRangedRating:
					ApplyRatingMod(CombatRating.CritRanged, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.CritSpellRating:
					ApplyRatingMod(CombatRating.CritSpell, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.CritTakenRangedRating:
					ApplyRatingMod(CombatRating.CritRanged, (int)val, apply);

					break;
				case ItemModType.HasteMeleeRating:
					ApplyRatingMod(CombatRating.HasteMelee, (int)val, apply);

					break;
				case ItemModType.HasteRangedRating:
					ApplyRatingMod(CombatRating.HasteRanged, (int)val, apply);

					break;
				case ItemModType.HasteSpellRating:
					ApplyRatingMod(CombatRating.HasteSpell, (int)val, apply);

					break;
				case ItemModType.HitRating:
					ApplyRatingMod(CombatRating.HitMelee, (int)(val * combatRatingMultiplier), apply);
					ApplyRatingMod(CombatRating.HitRanged, (int)(val * combatRatingMultiplier), apply);
					ApplyRatingMod(CombatRating.HitSpell, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.CritRating:
					ApplyRatingMod(CombatRating.CritMelee, (int)(val * combatRatingMultiplier), apply);
					ApplyRatingMod(CombatRating.CritRanged, (int)(val * combatRatingMultiplier), apply);
					ApplyRatingMod(CombatRating.CritSpell, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.ResilienceRating:
					ApplyRatingMod(CombatRating.ResiliencePlayerDamage, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.HasteRating:
					ApplyRatingMod(CombatRating.HasteMelee, (int)(val * combatRatingMultiplier), apply);
					ApplyRatingMod(CombatRating.HasteRanged, (int)(val * combatRatingMultiplier), apply);
					ApplyRatingMod(CombatRating.HasteSpell, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.ExpertiseRating:
					ApplyRatingMod(CombatRating.Expertise, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.AttackPower:
					HandleStatFlatModifier(UnitMods.AttackPower, UnitModifierFlatType.Total, (float)val, apply);
					HandleStatFlatModifier(UnitMods.AttackPowerRanged, UnitModifierFlatType.Total, (float)val, apply);

					break;
				case ItemModType.RangedAttackPower:
					HandleStatFlatModifier(UnitMods.AttackPowerRanged, UnitModifierFlatType.Total, (float)val, apply);

					break;
				case ItemModType.Versatility:
					ApplyRatingMod(CombatRating.VersatilityDamageDone, (int)(val * combatRatingMultiplier), apply);
					ApplyRatingMod(CombatRating.VersatilityDamageTaken, (int)(val * combatRatingMultiplier), apply);
					ApplyRatingMod(CombatRating.VersatilityHealingDone, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.ManaRegeneration:
					ApplyManaRegenBonus((int)val, apply);

					break;
				case ItemModType.ArmorPenetrationRating:
					ApplyRatingMod(CombatRating.ArmorPenetration, (int)val, apply);

					break;
				case ItemModType.SpellPower:
					ApplySpellPowerBonus((int)val, apply);

					break;
				case ItemModType.HealthRegen:
					ApplyHealthRegenBonus((int)val, apply);

					break;
				case ItemModType.SpellPenetration:
					ApplySpellPenetrationBonus((int)val, apply);

					break;
				case ItemModType.MasteryRating:
					ApplyRatingMod(CombatRating.Mastery, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.ExtraArmor:
					HandleStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Total, (float)val, apply);

					break;
				case ItemModType.FireResistance:
					HandleStatFlatModifier(UnitMods.ResistanceFire, UnitModifierFlatType.Base, (float)val, apply);

					break;
				case ItemModType.FrostResistance:
					HandleStatFlatModifier(UnitMods.ResistanceFrost, UnitModifierFlatType.Base, (float)val, apply);

					break;
				case ItemModType.HolyResistance:
					HandleStatFlatModifier(UnitMods.ResistanceHoly, UnitModifierFlatType.Base, (float)val, apply);

					break;
				case ItemModType.ShadowResistance:
					HandleStatFlatModifier(UnitMods.ResistanceShadow, UnitModifierFlatType.Base, (float)val, apply);

					break;
				case ItemModType.NatureResistance:
					HandleStatFlatModifier(UnitMods.ResistanceNature, UnitModifierFlatType.Base, (float)val, apply);

					break;
				case ItemModType.ArcaneResistance:
					HandleStatFlatModifier(UnitMods.ResistanceArcane, UnitModifierFlatType.Base, (float)val, apply);

					break;
				case ItemModType.PvpPower:
					ApplyRatingMod(CombatRating.PvpPower, (int)val, apply);

					break;
				case ItemModType.Corruption:
					ApplyRatingMod(CombatRating.Corruption, (int)val, apply);

					break;
				case ItemModType.CorruptionResistance:
					ApplyRatingMod(CombatRating.CorruptionResistance, (int)val, apply);

					break;
				case ItemModType.CrSpeed:
					ApplyRatingMod(CombatRating.Speed, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.CrLifesteal:
					ApplyRatingMod(CombatRating.Lifesteal, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.CrAvoidance:
					ApplyRatingMod(CombatRating.Avoidance, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.CrSturdiness:
					ApplyRatingMod(CombatRating.Studiness, (int)(val * combatRatingMultiplier), apply);

					break;
				case ItemModType.AgiStrInt:
					HandleStatFlatModifier(UnitMods.StatAgility, UnitModifierFlatType.Base, val, apply);
					HandleStatFlatModifier(UnitMods.StatStrength, UnitModifierFlatType.Base, val, apply);
					HandleStatFlatModifier(UnitMods.StatIntellect, UnitModifierFlatType.Base, val, apply);
					UpdateStatBuffMod(Stats.Agility);
					UpdateStatBuffMod(Stats.Strength);
					UpdateStatBuffMod(Stats.Intellect);

					break;
				case ItemModType.AgiStr:
					HandleStatFlatModifier(UnitMods.StatAgility, UnitModifierFlatType.Base, val, apply);
					HandleStatFlatModifier(UnitMods.StatStrength, UnitModifierFlatType.Base, val, apply);
					UpdateStatBuffMod(Stats.Agility);
					UpdateStatBuffMod(Stats.Strength);

					break;
				case ItemModType.AgiInt:
					HandleStatFlatModifier(UnitMods.StatAgility, UnitModifierFlatType.Base, val, apply);
					HandleStatFlatModifier(UnitMods.StatIntellect, UnitModifierFlatType.Base, val, apply);
					UpdateStatBuffMod(Stats.Agility);
					UpdateStatBuffMod(Stats.Intellect);

					break;
				case ItemModType.StrInt:
					HandleStatFlatModifier(UnitMods.StatStrength, UnitModifierFlatType.Base, val, apply);
					HandleStatFlatModifier(UnitMods.StatIntellect, UnitModifierFlatType.Base, val, apply);
					UpdateStatBuffMod(Stats.Strength);
					UpdateStatBuffMod(Stats.Intellect);

					break;
			}
		}

		var armor = proto.GetArmor(itemLevel);

		if (armor != 0)
		{
			HandleStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Total, (float)armor, apply);

			if (proto.Class == ItemClass.Armor && (ItemSubClassArmor)proto.SubClass == ItemSubClassArmor.Shield)
				SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ShieldBlock), apply ? (uint)(armor * 2.5f) : 0);
		}

		var attType = GetAttackBySlot(slot, proto.InventoryType);

		if (attType != WeaponAttackType.Max)
			_ApplyWeaponDamage(slot, item, apply);
	}

	public void ApplyEquipSpell(SpellInfo spellInfo, Item item, bool apply, bool formChange = false)
	{
		if (apply)
		{
			// Cannot be used in this stance/form
			if (spellInfo.CheckShapeshift(ShapeshiftForm) != SpellCastResult.SpellCastOk)
				return;

			if (formChange) // check aura active state from other form
				if (item != null)
				{
					if (GetAppliedAurasQuery().HasSpellId(spellInfo.Id).HasCastItemGuid(item.GUID).Results.Any())
						return;
				}
				else if (GetAppliedAurasQuery().HasSpellId(spellInfo.Id).Results.Any())
				{
					return;
				}

			Log.Logger.Debug("WORLD: cast {0} Equip spellId - {1}", (item != null ? "item" : "itemset"), spellInfo.Id);

			CastSpell(this, spellInfo.Id, new CastSpellExtraArgs(item));
		}
		else
		{
			if (formChange) // check aura compatibility
				// Cannot be used in this stance/form
				if (spellInfo.CheckShapeshift(ShapeshiftForm) == SpellCastResult.SpellCastOk)
					return; // and remove only not compatible at form change

			if (item != null)
				RemoveAurasDueToItemSpell(spellInfo.Id, item.GUID); // un-apply all spells, not only at-equipped
			else
				RemoveAura(spellInfo.Id); // un-apply spell (item set case)
		}
	}

	public void ApplyItemLootedSpell(Item item, bool apply)
	{
		if (item.Template.HasFlag(ItemFlags.Legacy))
			return;

		var lootedEffect = item.Effects.FirstOrDefault(effectData => effectData.TriggerType == ItemSpelltriggerType.OnLooted);

		if (lootedEffect != null)
		{
			if (apply)
				CastSpell(this, (uint)lootedEffect.SpellID, item);
			else
				RemoveAurasDueToItemSpell((uint)lootedEffect.SpellID, item.GUID);
		}
	}

	public void _ApplyAllLevelScaleItemMods(bool apply)
	{
		for (byte i = 0; i < InventorySlots.BagEnd; ++i)
			if (_items[i] != null)
			{
				if (!CanUseAttackType(GetAttackBySlot(i, _items[i].Template.InventoryType)))
					continue;

				_ApplyItemMods(_items[i], i, apply);

				// Update item sets for heirlooms
				if (Global.DB2Mgr.GetHeirloomByItemId(_items[i].Entry) != null && _items[i].Template.ItemSet != 0)
				{
					if (apply)
						Item.AddItemsSetItem(this, _items[i]);
					else
						Item.RemoveItemsSetItem(this, _items[i]);
				}
			}
	}

	public Loot.Loot GetLootByWorldObjectGUID(ObjectGuid lootWorldObjectGuid)
	{
		return _aeLootView.FirstOrDefault(pair => pair.Value.GetOwnerGUID() == lootWorldObjectGuid).Value;
	}

	public LootRoll GetLootRoll(ObjectGuid lootObjectGuid, byte lootListId)
	{
		return _lootRolls.Find(roll => roll.IsLootItem(lootObjectGuid, lootListId));
	}

	public void AddLootRoll(LootRoll roll)
	{
		_lootRolls.Add(roll);
	}

	public void RemoveLootRoll(LootRoll roll)
	{
		_lootRolls.Remove(roll);
	}

	//Inventory
	public bool IsInventoryPos(ushort pos)
	{
		return IsInventoryPos((byte)(pos >> 8), (byte)(pos & 255));
	}

	public static bool IsInventoryPos(byte bag, byte slot)
	{
		if (bag == InventorySlots.Bag0 && slot == ItemConst.NullSlot)
			return true;

		if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ItemStart && slot < InventorySlots.ItemEnd))
			return true;

		if (bag >= InventorySlots.BagStart && bag < InventorySlots.BagEnd)
			return true;

		if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ReagentStart && slot < InventorySlots.ReagentEnd))
			return true;

		if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ChildEquipmentStart && slot < InventorySlots.ChildEquipmentEnd))
			return true;

		return false;
	}

	public void MoveItemFromInventory(byte bag, byte slot, bool update)
	{
		var it = GetItemByPos(bag, slot);

		if (it != null)
		{
			RemoveItem(bag, slot, update);
			ItemRemovedQuestCheck(it.Entry, it.Count);
			it.SetNotRefundable(this, false, null, false);
			Item.RemoveItemFromUpdateQueueOf(it, this);
			Session.CollectionMgr.RemoveTemporaryAppearance(it);

			if (it.IsInWorld)
			{
				it.RemoveFromWorld();
				it.DestroyForPlayer(this);
			}
		}
	}

	public void MoveItemToInventory(List<ItemPosCount> dest, Item pItem, bool update, bool in_characterInventoryDB = false)
	{
		var itemId = pItem.Entry;
		var count = pItem.Count;

		// store item
		var pLastItem = StoreItem(dest, pItem, update);

		// only set if not merged to existed stack
		if (pLastItem == pItem)
		{
			// update owner for last item (this can be original item with wrong owner
			if (pLastItem.OwnerGUID != GUID)
				pLastItem.SetOwnerGUID(GUID);

			// if this original item then it need create record in inventory
			// in case trade we already have item in other player inventory
			pLastItem.SetState(in_characterInventoryDB ? ItemUpdateState.Changed : ItemUpdateState.New, this);

			if (pLastItem.IsBOPTradeable)
				AddTradeableItem(pLastItem);
		}

		// update quest counters
		ItemAddedQuestCheck(itemId, count);
		UpdateCriteria(CriteriaType.ObtainAnyItem, itemId, count);
	}

	//Bank
	public static bool IsBankPos(ushort pos)
	{
		return IsBankPos((byte)(pos >> 8), (byte)(pos & 255));
	}

	public static bool IsBankPos(byte bag, byte slot)
	{
		if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BankItemStart && slot < InventorySlots.BankItemEnd))
			return true;

		if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BankBagStart && slot < InventorySlots.BankBagEnd))
			return true;

		if (bag >= InventorySlots.BankBagStart && bag < InventorySlots.BankBagEnd)
			return true;

		if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ReagentStart && slot < InventorySlots.ReagentEnd))
			return true;

		return false;
	}

	public InventoryResult CanBankItem(byte bag, byte slot, List<ItemPosCount> dest, Item pItem, bool swap, bool not_loading = true, bool reagentBankOnly = false)
	{
		if (pItem == null)
			return swap ? InventoryResult.CantSwap : InventoryResult.ItemNotFound;

		// different slots range if we're trying to store item in Reagent Bank
		if ((IsReagentBankPos(bag, slot) || reagentBankOnly) && !IsReagentBankUnlocked)
			return InventoryResult.ReagentBankLocked;

		var slotStart = reagentBankOnly ? InventorySlots.ReagentStart : InventorySlots.BankItemStart;
		var slotEnd = reagentBankOnly ? InventorySlots.ReagentEnd : InventorySlots.BankItemEnd;

		var count = pItem.Count;

		Log.Logger.Debug("STORAGE: CanBankItem bag = {0}, slot = {1}, item = {2}, count = {3}", bag, slot, pItem.Entry, count);
		var pProto = pItem.Template;

		if (pProto == null)
			return swap ? InventoryResult.CantSwap : InventoryResult.ItemNotFound;

		// item used
		if (pItem.LootGenerated)
			return InventoryResult.LootGone;

		if (pItem.IsBindedNotWith(this))
			return InventoryResult.NotOwner;

		// Currency tokens are not supposed to be swapped out of their hidden bag
		if (pItem.IsCurrencyToken)
		{
			Log.Logger.Error("Possible hacking attempt: Player {0} [guid: {1}] tried to move token [guid: {2}, entry: {3}] out of the currency bag!",
							GetName(),
							GUID.ToString(),
							pItem.GUID.ToString(),
							pProto.Id);

			return InventoryResult.CantSwap;
		}

		// check count of items (skip for auto move for same player from bank)
		var res = CanTakeMoreSimilarItems(pItem);

		if (res != InventoryResult.Ok)
			return res;

		// in specific slot
		if (bag != ItemConst.NullBag && slot != ItemConst.NullSlot)
		{
			if (slot >= InventorySlots.BagStart && slot < InventorySlots.BagEnd)
			{
				if (!pItem.IsBag)
					return InventoryResult.WrongSlot;

				if (slot - InventorySlots.BagStart >= GetBankBagSlotCount())
					return InventoryResult.NoBankSlot;

				res = CanUseItem(pItem, not_loading);

				if (res != InventoryResult.Ok)
					return res;
			}

			res = CanStoreItem_InSpecificSlot(bag, slot, dest, pProto, ref count, swap, pItem);

			if (res != InventoryResult.Ok)
				return res;

			if (count == 0)
				return InventoryResult.Ok;
		}

		// not specific slot or have space for partly store only in specific slot

		// in specific bag
		if (bag != ItemConst.NullBag)
		{
			if (pItem.IsNotEmptyBag)
				return InventoryResult.BagInBag;

			// search stack in bag for merge to
			if (pProto.MaxStackSize != 1)
			{
				if (bag == InventorySlots.Bag0)
				{
					res = CanStoreItem_InInventorySlots(slotStart, slotEnd, dest, pProto, ref count, true, pItem, bag, slot);

					if (res != InventoryResult.Ok)
						return res;

					if (count == 0)
						return InventoryResult.Ok;
				}
				else
				{
					res = CanStoreItem_InBag(bag, dest, pProto, ref count, true, false, pItem, ItemConst.NullBag, slot);

					if (res != InventoryResult.Ok)
						res = CanStoreItem_InBag(bag, dest, pProto, ref count, true, true, pItem, ItemConst.NullBag, slot);

					if (res != InventoryResult.Ok)
						return res;

					if (count == 0)
						return InventoryResult.Ok;
				}
			}

			// search free slot in bag
			if (bag == InventorySlots.Bag0)
			{
				res = CanStoreItem_InInventorySlots(slotStart, slotEnd, dest, pProto, ref count, false, pItem, bag, slot);

				if (res != InventoryResult.Ok)
					return res;

				if (count == 0)
					return InventoryResult.Ok;
			}
			else
			{
				res = CanStoreItem_InBag(bag, dest, pProto, ref count, false, false, pItem, ItemConst.NullBag, slot);

				if (res != InventoryResult.Ok)
					res = CanStoreItem_InBag(bag, dest, pProto, ref count, false, true, pItem, ItemConst.NullBag, slot);

				if (res != InventoryResult.Ok)
					return res;

				if (count == 0)
					return InventoryResult.Ok;
			}
		}

		// not specific bag or have space for partly store only in specific bag

		// search stack for merge to
		if (pProto.MaxStackSize != 1)
		{
			// in slots
			res = CanStoreItem_InInventorySlots(slotStart, slotEnd, dest, pProto, ref count, true, pItem, bag, slot);

			if (res != InventoryResult.Ok)
				return res;

			if (count == 0)
				return InventoryResult.Ok;

			// don't try to store reagents anywhere else than in Reagent Bank if we're on it
			if (!reagentBankOnly)
			{
				// in special bags
				if (pProto.BagFamily != BagFamilyMask.None)
					for (var i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; i++)
					{
						res = CanStoreItem_InBag(i, dest, pProto, ref count, true, false, pItem, bag, slot);

						if (res != InventoryResult.Ok)
							continue;

						if (count == 0)
							return InventoryResult.Ok;
					}

				// in regular bags
				for (var i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; i++)
				{
					res = CanStoreItem_InBag(i, dest, pProto, ref count, true, true, pItem, bag, slot);

					if (res != InventoryResult.Ok)
						continue;

					if (count == 0)
						return InventoryResult.Ok;
				}
			}
		}

		// search free place in special bag
		if (!reagentBankOnly && pProto.BagFamily != BagFamilyMask.None)
			for (var i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; i++)
			{
				res = CanStoreItem_InBag(i, dest, pProto, ref count, false, false, pItem, bag, slot);

				if (res != InventoryResult.Ok)
					continue;

				if (count == 0)
					return InventoryResult.Ok;
			}

		// search free space
		res = CanStoreItem_InInventorySlots(slotStart, slotEnd, dest, pProto, ref count, false, pItem, bag, slot);

		if (res != InventoryResult.Ok)
			return res;

		if (count == 0)
			return InventoryResult.Ok;

		// search free space in regular bags (don't try to store reagents anywhere else than in Reagent Bank if we're on it)
		if (!reagentBankOnly)
			for (var i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; i++)
			{
				res = CanStoreItem_InBag(i, dest, pProto, ref count, false, true, pItem, bag, slot);

				if (res != InventoryResult.Ok)
					continue;

				if (count == 0)
					return InventoryResult.Ok;
			}

		return reagentBankOnly ? InventoryResult.ReagentBankFull : InventoryResult.BankFull;
	}

	public Item BankItem(List<ItemPosCount> dest, Item pItem, bool update)
	{
		return StoreItem(dest, pItem, update);
	}

	public uint GetFreeInventorySlotCount(ItemSearchLocation location = ItemSearchLocation.Inventory)
	{
		uint freeSlotCount = 0;

		if (location.HasFlag(ItemSearchLocation.Equipment))
		{
			for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
				if (GetItemByPos(InventorySlots.Bag0, i) == null)
					++freeSlotCount;

			for (var i = ProfessionSlots.Start; i < ProfessionSlots.End; ++i)
				if (!GetItemByPos(InventorySlots.Bag0, i))
					++freeSlotCount;
		}

		if (location.HasFlag(ItemSearchLocation.Inventory))
		{
			var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

			for (var i = InventorySlots.ItemStart; i < inventoryEnd; ++i)
				if (GetItemByPos(InventorySlots.Bag0, i) == null)
					++freeSlotCount;

			for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
			{
				var bag = GetBagByPos(i);

				if (bag != null)
					for (byte j = 0; j < bag.GetBagSize(); ++j)
						if (bag.GetItemByPos(j) == null)
							++freeSlotCount;
			}
		}

		if (location.HasFlag(ItemSearchLocation.Bank))
		{
			for (var i = InventorySlots.BankItemStart; i < InventorySlots.BankItemEnd; ++i)
				if (GetItemByPos(InventorySlots.Bag0, i) == null)
					++freeSlotCount;

			for (var i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; ++i)
			{
				var bag = GetBagByPos(i);

				if (bag != null)
					for (byte j = 0; j < bag.GetBagSize(); ++j)
						if (bag.GetItemByPos(j) == null)
							++freeSlotCount;
			}
		}

		if (location.HasFlag(ItemSearchLocation.ReagentBank))
		{
			for (var i = InventorySlots.ReagentBagStart; i < InventorySlots.ReagentBagEnd; ++i)
			{
				var bag = GetBagByPos(i);

				if (bag != null)
					for (byte j = 0; j < bag.GetBagSize(); ++j)
						if (bag.GetItemByPos(j) == null)
							++freeSlotCount;
			}

			for (var i = InventorySlots.ReagentStart; i < InventorySlots.ReagentEnd; ++i)
				if (GetItemByPos(InventorySlots.Bag0, i) == null)
					++freeSlotCount;
		}

		return freeSlotCount;
	}

	public uint GetFreeInventorySpace()
	{
		uint freeSpace = 0;

		// Check backpack
		for (var slot = InventorySlots.ItemStart; slot < InventorySlots.ItemEnd; ++slot)
		{
			var item = GetItemByPos(InventorySlots.Bag0, slot);

			if (item == null)
				freeSpace += 1;
		}

		// Check bags
		for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
		{
			var bag = GetBagByPos(i);

			if (bag != null)
				freeSpace += bag.GetFreeSlots();
		}

		return freeSpace;
	}

	//Reagent
	public static bool IsReagentBankPos(ushort pos)
	{
		return IsReagentBankPos((byte)(pos >> 8), (byte)(pos & 255));
	}

	public static bool IsReagentBankPos(byte bag, byte slot)
	{
		if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ReagentStart && slot < InventorySlots.ReagentEnd))
			return true;

		return false;
	}

	//Bags
	public Bag GetBagByPos(byte bag)
	{
		if ((bag >= InventorySlots.BagStart && bag < InventorySlots.BagEnd) || (bag >= InventorySlots.BankBagStart && bag < InventorySlots.BankBagEnd) || (bag >= InventorySlots.ReagentBagStart && bag < InventorySlots.ReagentBagEnd))
		{
			var item = GetItemByPos(InventorySlots.Bag0, bag);

			if (item != null)
				return item.AsBag;
		}

		return null;
	}

	public static bool IsBagPos(ushort pos)
	{
		var bag = (byte)(pos >> 8);
		var slot = (byte)(pos & 255);

		if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BagStart && slot < InventorySlots.BagEnd))
			return true;

		if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BankBagStart && slot < InventorySlots.BankBagEnd))
			return true;

		if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ReagentBagStart && slot < InventorySlots.ReagentBagEnd))
			return true;

		return false;
	}

	//Equipment
	public static bool IsEquipmentPos(ushort pos)
	{
		return IsEquipmentPos((byte)(pos >> 8), (byte)(pos & 255));
	}

	public static bool IsEquipmentPos(byte bag, byte slot)
	{
		if (bag == InventorySlots.Bag0 && (slot < EquipmentSlot.End))
			return true;

		if (bag == InventorySlots.Bag0 && (slot >= ProfessionSlots.Start && slot < ProfessionSlots.End))
			return true;

		if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.BagStart && slot < InventorySlots.BagEnd))
			return true;

		if (bag == InventorySlots.Bag0 && (slot >= InventorySlots.ReagentBagStart && slot < InventorySlots.ReagentBagEnd))
			return true;

		return false;
	}

	public InventoryResult CanEquipItem(byte slot, out ushort dest, Item pItem, bool swap, bool not_loading = true)
	{
		dest = 0;

		if (pItem != null)
		{
			Log.Logger.Debug("STORAGE: CanEquipItem slot = {0}, item = {1}, count = {2}", slot, pItem.Entry, pItem.Count);
			var pProto = pItem.Template;

			if (pProto != null)
			{
				// item used
				if (pItem.LootGenerated)
					return InventoryResult.LootGone;

				if (pItem.IsBindedNotWith(this))
					return InventoryResult.NotOwner;

				// check count of items (skip for auto move for same player from bank)
				var res = CanTakeMoreSimilarItems(pItem);

				if (res != InventoryResult.Ok)
					return res;

				// check this only in game
				if (not_loading)
				{
					// May be here should be more stronger checks; STUNNED checked
					// ROOT, CONFUSED, DISTRACTED, FLEEING this needs to be checked.
					if (HasUnitState(UnitState.Stunned))
						return InventoryResult.GenericStunned;

					if (IsCharmed)
						return InventoryResult.CantDoThatRightNow; // @todo is this the correct error?

					// do not allow equipping gear except weapons, offhands, projectiles, relics in
					// - combat
					// - in-progress arenas
					if (!pProto.CanChangeEquipStateInCombat())
					{
						if (IsInCombat)
							return InventoryResult.NotInCombat;

						var bg = Battleground;

						if (bg)
							if (bg.IsArena() && bg.GetStatus() == BattlegroundStatus.InProgress)
								return InventoryResult.NotDuringArenaMatch;
					}

					if (IsInCombat && (pProto.Class == ItemClass.Weapon || pProto.InventoryType == InventoryType.Relic) && _weaponChangeTimer != 0)
						return InventoryResult.ItemCooldown;

					var currentGenericSpell = GetCurrentSpell(CurrentSpellTypes.Generic);

					if (currentGenericSpell != null)
						if (!currentGenericSpell.SpellInfo.HasAttribute(SpellAttr6.AllowEquipWhileCasting))
							return InventoryResult.ClientLockedOut;

					var currentChanneledSpell = GetCurrentSpell(CurrentSpellTypes.Channeled);

					if (currentChanneledSpell != null)
						if (!currentChanneledSpell.SpellInfo.HasAttribute(SpellAttr6.AllowEquipWhileCasting))
							return InventoryResult.ClientLockedOut;
				}

				ContentTuningLevels? requiredLevels = null;

				// check allowed level (extend range to upper values if MaxLevel more or equal max player level, this let GM set high level with 1...max range items)
				if (pItem.Quality == ItemQuality.Heirloom)
					requiredLevels = Global.DB2Mgr.GetContentTuningData(pItem.ScalingContentTuningId, 0, true);

				if (requiredLevels.HasValue && requiredLevels.Value.MaxLevel < SharedConst.DefaultMaxLevel && requiredLevels.Value.MaxLevel < Level && Global.DB2Mgr.GetHeirloomByItemId(pProto.Id) == null)
					return InventoryResult.NotEquippable;

				var eslot = FindEquipSlot(pItem, slot, swap);

				if (eslot == ItemConst.NullSlot)
					return InventoryResult.NotEquippable;

				res = CanUseItem(pItem, not_loading);

				if (res != InventoryResult.Ok)
					return res;

				if (!swap && GetItemByPos(InventorySlots.Bag0, eslot) != null)
					return InventoryResult.NoSlotAvailable;

				// if we are swapping 2 equiped items, CanEquipUniqueItem check
				// should ignore the item we are trying to swap, and not the
				// destination item. CanEquipUniqueItem should ignore destination
				// item only when we are swapping weapon from bag
				var ignore = ItemConst.NullSlot;

				switch (eslot)
				{
					case EquipmentSlot.MainHand:
						ignore = EquipmentSlot.OffHand;

						break;
					case EquipmentSlot.OffHand:
						ignore = EquipmentSlot.MainHand;

						break;
					case EquipmentSlot.Finger1:
						ignore = EquipmentSlot.Finger2;

						break;
					case EquipmentSlot.Finger2:
						ignore = EquipmentSlot.Finger1;

						break;
					case EquipmentSlot.Trinket1:
						ignore = EquipmentSlot.Trinket2;

						break;
					case EquipmentSlot.Trinket2:
						ignore = EquipmentSlot.Trinket1;

						break;
					case ProfessionSlots.Profession1Gear1:
						ignore = ProfessionSlots.Profession1Gear2;

						break;
					case ProfessionSlots.Profession1Gear2:
						ignore = ProfessionSlots.Profession1Gear1;

						break;
					case ProfessionSlots.Profession2Gear1:
						ignore = ProfessionSlots.Profession2Gear2;

						break;
					case ProfessionSlots.Profession2Gear2:
						ignore = ProfessionSlots.Profession2Gear1;

						break;
				}

				if (ignore == ItemConst.NullSlot || pItem != GetItemByPos(InventorySlots.Bag0, ignore))
					ignore = eslot;

				// if swap ignore item (equipped also)
				var res2 = CanEquipUniqueItem(pItem, swap ? ignore : ItemConst.NullSlot);

				if (res2 != InventoryResult.Ok)
					return res2;

				// check unique-equipped special item classes
				if (pProto.Class == ItemClass.Quiver)
					for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; ++i)
					{
						var pBag = GetItemByPos(InventorySlots.Bag0, i);

						if (pBag != null)
							if (pBag != pItem)
							{
								var pBagProto = pBag.Template;

								if (pBagProto != null)
									if (pBagProto.Class == pProto.Class && (!swap || pBag.Slot != eslot))
										return (pBagProto.SubClass == (uint)ItemSubClassQuiver.AmmoPouch)
													? InventoryResult.OnlyOneAmmo
													: InventoryResult.OnlyOneQuiver;
							}
					}

				var type = pProto.InventoryType;

				if (eslot == EquipmentSlot.OffHand)
				{
					// Do not allow polearm to be equipped in the offhand (rare case for the only 1h polearm 41750)
					if (type == InventoryType.Weapon && pProto.SubClass == (uint)ItemSubClassWeapon.Polearm)
					{
						return InventoryResult.TwoHandSkillNotFound;
					}
					else if (type == InventoryType.Weapon)
					{
						if (!CanDualWield)
							return InventoryResult.TwoHandSkillNotFound;
					}
					else if (type == InventoryType.WeaponOffhand)
					{
						if (!CanDualWield && !pProto.HasFlag(ItemFlags3.AlwaysAllowDualWield))
							return InventoryResult.TwoHandSkillNotFound;
					}
					else if (type == InventoryType.Weapon2Hand)
					{
						if (!CanDualWield || !CanTitanGrip())
							return InventoryResult.TwoHandSkillNotFound;
					}

					if (IsTwoHandUsed())
						return InventoryResult.Equipped2handed;
				}

				// equip two-hand weapon case (with possible unequip 2 items)
				if (type == InventoryType.Weapon2Hand)
				{
					if (eslot == EquipmentSlot.OffHand)
					{
						if (!CanTitanGrip())
							return InventoryResult.NotEquippable;
					}
					else if (eslot != EquipmentSlot.MainHand)
					{
						return InventoryResult.NotEquippable;
					}

					if (!CanTitanGrip())
					{
						// offhand item must can be stored in inventory for offhand item and it also must be unequipped
						var offItem = GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
						List<ItemPosCount> off_dest = new();

						if (offItem != null &&
							(!not_loading ||
							CanUnequipItem(((int)InventorySlots.Bag0 << 8) | (int)EquipmentSlot.OffHand, false) != InventoryResult.Ok ||
							CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, off_dest, offItem, false) != InventoryResult.Ok))
							return swap ? InventoryResult.CantSwap : InventoryResult.InvFull;
					}
				}

				dest = (ushort)(((uint)InventorySlots.Bag0 << 8) | eslot);

				return InventoryResult.Ok;
			}
		}

		return !swap ? InventoryResult.ItemNotFound : InventoryResult.CantSwap;
	}

	public InventoryResult CanEquipChildItem(Item parentItem)
	{
		var childItem = GetChildItemByGuid(parentItem.ChildItem);

		if (!childItem)
			return InventoryResult.Ok;

		var childEquipement = Global.DB2Mgr.GetItemChildEquipment(parentItem.Entry);

		if (childEquipement == null)
			return InventoryResult.Ok;

		var dstItem = GetItemByPos(InventorySlots.Bag0, childEquipement.ChildItemEquipSlot);

		if (!dstItem)
			return InventoryResult.Ok;

		var childDest = (ushort)((InventorySlots.Bag0 << 8) | childEquipement.ChildItemEquipSlot);
		var msg = CanUnequipItem(childDest, !childItem.IsBag);

		if (msg != InventoryResult.Ok)
			return msg;

		// check dest.src move possibility
		var src = parentItem.Pos;
		List<ItemPosCount> dest = new();

		if (IsInventoryPos(src))
		{
			msg = CanStoreItem(parentItem.BagSlot, ItemConst.NullSlot, dest, dstItem, true);

			if (msg != InventoryResult.Ok)
				msg = CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, dstItem, true);
		}
		else if (IsBankPos(src))
		{
			msg = CanBankItem(parentItem.BagSlot, ItemConst.NullSlot, dest, dstItem, true);

			if (msg != InventoryResult.Ok)
				msg = CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, dstItem, true);
		}
		else if (IsEquipmentPos(src))
		{
			return InventoryResult.CantSwap;
		}

		return msg;
	}

	public InventoryResult CanEquipUniqueItem(Item pItem, byte eslot = ItemConst.NullSlot, uint limit_count = 1)
	{
		var pProto = pItem.Template;

		// proto based limitations
		var res = CanEquipUniqueItem(pProto, eslot, limit_count);

		if (res != InventoryResult.Ok)
			return res;

		// check unique-equipped on gems
		foreach (var gemData in pItem.ItemData.Gems)
		{
			var pGem = Global.ObjectMgr.GetItemTemplate(gemData.ItemId);

			if (pGem == null)
				continue;

			// include for check equip another gems with same limit category for not equipped item (and then not counted)
			var gem_limit_count = (uint)(!pItem.IsEquipped && pGem.ItemLimitCategory != 0 ? pItem.GetGemCountWithLimitCategory(pGem.ItemLimitCategory) : 1);

			var ress = CanEquipUniqueItem(pGem, eslot, gem_limit_count);

			if (ress != InventoryResult.Ok)
				return ress;
		}

		return InventoryResult.Ok;
	}

	public InventoryResult CanEquipUniqueItem(ItemTemplate itemProto, byte except_slot = ItemConst.NullSlot, uint limit_count = 1)
	{
		// check unique-equipped on item
		if (itemProto.HasFlag(ItemFlags.UniqueEquippable))
			// there is an equip limit on this item
			if (HasItemOrGemWithIdEquipped(itemProto.Id, 1, except_slot))
				return InventoryResult.ItemUniqueEquippable;

		// check unique-equipped limit
		if (itemProto.ItemLimitCategory != 0)
		{
			var limitEntry = CliDB.ItemLimitCategoryStorage.LookupByKey(itemProto.ItemLimitCategory);

			if (limitEntry == null)
				return InventoryResult.NotEquippable;

			// NOTE: limitEntry.mode not checked because if item have have-limit then it applied and to equip case
			var limitQuantity = GetItemLimitCategoryQuantity(limitEntry);

			if (limit_count > limitQuantity)
				return InventoryResult.ItemMaxLimitCategoryEquippedExceededIs;

			// there is an equip limit on this item
			if (HasItemWithLimitCategoryEquipped(itemProto.ItemLimitCategory, limitQuantity - limit_count + 1, except_slot))
				return InventoryResult.ItemMaxLimitCategoryEquippedExceededIs;
			else if (HasGemWithLimitCategoryEquipped(itemProto.ItemLimitCategory, limitQuantity - limit_count + 1, except_slot))
				return InventoryResult.ItemMaxCountEquippedSocketed;
		}

		return InventoryResult.Ok;
	}

	public InventoryResult CanUnequipItem(ushort pos, bool swap)
	{
		// Applied only to equipped items and bank bags
		if (!IsEquipmentPos(pos) && !IsBagPos(pos))
			return InventoryResult.Ok;

		var pItem = GetItemByPos(pos);

		// Applied only to existed equipped item
		if (pItem == null)
			return InventoryResult.Ok;

		Log.Logger.Debug("STORAGE: CanUnequipItem slot = {0}, item = {1}, count = {2}", pos, pItem.Entry, pItem.Count);

		var pProto = pItem.Template;

		if (pProto == null)
			return InventoryResult.ItemNotFound;

		// item used
		if (pItem.LootGenerated)
			return InventoryResult.LootGone;

		if (IsCharmed)
			return InventoryResult.CantDoThatRightNow; // @todo is this the correct error?

		// do not allow unequipping gear except weapons, offhands, projectiles, relics in
		// - combat
		// - in-progress arenas
		if (!pProto.CanChangeEquipStateInCombat())
		{
			if (IsInCombat)
				return InventoryResult.NotInCombat;

			var bg = Battleground;

			if (bg)
				if (bg.IsArena() && bg.GetStatus() == BattlegroundStatus.InProgress)
					return InventoryResult.NotDuringArenaMatch;
		}

		if (!swap && pItem.IsNotEmptyBag)
			return InventoryResult.DestroyNonemptyBag;

		return InventoryResult.Ok;
	}

	//Child
	public static bool IsChildEquipmentPos(ushort pos)
	{
		return IsChildEquipmentPos((byte)(pos >> 8), (byte)(pos & 255));
	}

	public void ApplyArtifactPowerRank(Item artifact, ArtifactPowerRankRecord artifactPowerRank, bool apply)
	{
		var spellInfo = Global.SpellMgr.GetSpellInfo(artifactPowerRank.SpellID, Difficulty.None);

		if (spellInfo == null)
			return;

		if (spellInfo.IsPassive)
		{
			var powerAura = GetAuraApplication(artifactPowerRank.SpellID, ObjectGuid.Empty, artifact.GUID);

			if (powerAura != null)
			{
				if (apply)
				{
					foreach (var auraEffect in powerAura.Base.AuraEffects)
						if (powerAura.HasEffect(auraEffect.Value.EffIndex))
							auraEffect.Value.ChangeAmount((int)(artifactPowerRank.AuraPointsOverride != 0 ? artifactPowerRank.AuraPointsOverride : auraEffect.Value.GetSpellEffectInfo().CalcValue()));
				}
				else
				{
					RemoveAura(powerAura);
				}
			}
			else if (apply)
			{
				CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
				args.SetCastItem(artifact);

				if (artifactPowerRank.AuraPointsOverride != 0)
					foreach (var spellEffectInfo in spellInfo.Effects)
						args.AddSpellMod(SpellValueMod.BasePoint0 + spellEffectInfo.EffectIndex, (int)artifactPowerRank.AuraPointsOverride);

				CastSpell(this, artifactPowerRank.SpellID, args);
			}
		}
		else
		{
			if (apply && !HasSpell(artifactPowerRank.SpellID))
			{
				AddTemporarySpell(artifactPowerRank.SpellID);
				LearnedSpells learnedSpells = new();
				LearnedSpellInfo learnedSpellInfo = new();

				{
					SpellID = artifactPowerRank.SpellID
				}

				rnedSpells.SuppressMessaging = true;
				learnedSpells.ClientLearnedSpellData.Add(learnedSpellInfo);
				SendPacket(learnedSpells);
			}
			else if (!apply)
			{
				RemoveTemporarySpell(artifactPowerRank.SpellID);
				UnlearnedSpells unlearnedSpells = new();

				{
					SuppressMessaging = true
				}

				earnedSpells.SpellID.Add(artifactPowerRank.SpellID);
				SendPacket(unlearnedSpells);
			}
		}
	}

	public void ApplyAzeriteItemMilestonePower(AzeriteItem item, AzeriteItemMilestonePowerRecord azeriteItemMilestonePower, bool apply)
	{
		var type = (AzeriteItemMilestoneType)azeriteItemMilestonePower.Type;

		if (type == AzeriteItemMilestoneType.BonusStamina)
		{
			var azeritePower = CliDB.AzeritePowerStorage.LookupByKey(azeriteItemMilestonePower.AzeritePowerID);

			if (azeritePower != null)
			{
				if (apply)
					CastSpell(this, azeritePower.SpellID, item);
				else
					RemoveAurasDueToItemSpell(azeritePower.SpellID, item.GUID);
			}
		}
	}

	public void ApplyAzeriteEssence(AzeriteItem item, uint azeriteEssenceId, uint rank, bool major, bool apply)
	{
		for (uint currentRank = 1; currentRank <= rank; ++currentRank)
		{
			var azeriteEssencePower = Global.DB2Mgr.GetAzeriteEssencePower(azeriteEssenceId, currentRank);

			if (azeriteEssencePower != null)
			{
				ApplyAzeriteEssencePower(item, azeriteEssencePower, major, apply);

				if (major && currentRank == 1)
				{
					if (apply)
					{
						CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
						args.AddSpellMod(SpellValueMod.BasePoint0, (int)azeriteEssencePower.MajorPowerDescription);
						CastSpell(this, PlayerConst.SpellIdHeartEssenceActionBarOverride, args);
					}
					else
					{
						RemoveAura(PlayerConst.SpellIdHeartEssenceActionBarOverride);
					}
				}
			}
		}
	}

	public void ApplyAzeritePower(AzeriteEmpoweredItem item, AzeritePowerRecord azeritePower, bool apply)
	{
		if (apply)
		{
			if (azeritePower.SpecSetID == 0 || Global.DB2Mgr.IsSpecSetMember(azeritePower.SpecSetID, GetPrimarySpecialization()))
				CastSpell(this, azeritePower.SpellID, item);
		}
		else
		{
			RemoveAurasDueToItemSpell(azeritePower.SpellID, item.GUID);
		}
	}

	public bool HasItemOrGemWithIdEquipped(uint item, uint count, byte except_slot = ItemConst.NullSlot)
	{
		uint tempcount = 0;

		var pProto = Global.ObjectMgr.GetItemTemplate(item);
		var includeGems = pProto?.GemProperties != 0;

		return !ForEachItem(ItemSearchLocation.Equipment,
							pItem =>
							{
								if (pItem.Slot != except_slot)
								{
									if (pItem.Entry == item)
										tempcount += pItem.Count;

									if (includeGems)
										tempcount += pItem.GetGemCountWithID(item);

									if (tempcount >= count)
										return false;
								}

								return true;
							});
	}

	//Visual
	public void SetVisibleItemSlot(uint slot, Item pItem)
	{
		var itemField = Values.ModifyValue(PlayerData).ModifyValue(PlayerData.VisibleItems, (int)slot);

		if (pItem != null)
		{
			SetUpdateFieldValue(itemField.ModifyValue(itemField.ItemID), pItem.GetVisibleEntry(this));
			SetUpdateFieldValue(itemField.ModifyValue(itemField.SecondaryItemModifiedAppearanceID), pItem.GetVisibleSecondaryModifiedAppearanceId(this));
			SetUpdateFieldValue(itemField.ModifyValue(itemField.ItemAppearanceModID), pItem.GetVisibleAppearanceModId(this));
			SetUpdateFieldValue(itemField.ModifyValue(itemField.ItemVisual), pItem.GetVisibleItemVisual(this));
		}
		else
		{
			SetUpdateFieldValue(itemField.ModifyValue(itemField.ItemID), 0u);
			SetUpdateFieldValue(itemField.ModifyValue(itemField.SecondaryItemModifiedAppearanceID), 0u);
			SetUpdateFieldValue(itemField.ModifyValue(itemField.ItemAppearanceModID), (ushort)0);
			SetUpdateFieldValue(itemField.ModifyValue(itemField.ItemVisual), (ushort)0);
		}
	}

	public void DestroyItem(byte bag, byte slot, bool update)
	{
		var pItem = GetItemByPos(bag, slot);

		if (pItem != null)
		{
			Log.Logger.Debug("STORAGE: DestroyItem bag = {0}, slot = {1}, item = {2}", bag, slot, pItem.Entry);

			// Also remove all contained items if the item is a bag.
			// This if () prevents item saving crashes if the condition for a bag to be empty before being destroyed was bypassed somehow.
			if (pItem.IsNotEmptyBag)
				for (byte i = 0; i < ItemConst.MaxBagSize; ++i)
					DestroyItem(slot, i, update);

			if (pItem.IsWrapped)
			{
				var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GIFT);
				stmt.AddValue(0, pItem.GUID.Counter);
				DB.Characters.Execute(stmt);
			}

			RemoveEnchantmentDurations(pItem);
			RemoveItemDurations(pItem);

			pItem.SetNotRefundable(this);
			pItem.ClearSoulboundTradeable(this);
			RemoveTradeableItem(pItem);

			ApplyItemObtainSpells(pItem, false);
			ApplyItemLootedSpell(pItem, false);
			Global.ScriptMgr.RunScriptRet<IItemOnRemove>(tmpscript => tmpscript.OnRemove(this, pItem), pItem.ScriptId);

			Bag pBag;
			var pProto = pItem.Template;

			if (bag == InventorySlots.Bag0)
			{
				SetInvSlot(slot, ObjectGuid.Empty);

				// equipment and equipped bags can have applied bonuses
				if (slot < InventorySlots.BagEnd)
				{
					// item set bonuses applied only at equip and removed at unequip, and still active for broken items
					if (pProto != null && pProto.ItemSet != 0)
						Item.RemoveItemsSetItem(this, pItem);

					_ApplyItemMods(pItem, slot, false);
				}

				if (slot < EquipmentSlot.End)
				{
					// update expertise and armor penetration - passive auras may need it
					switch (slot)
					{
						case EquipmentSlot.MainHand:
						case EquipmentSlot.OffHand:
							RecalculateRating(CombatRating.ArmorPenetration);

							break;
						default:
							break;
					}

					if (slot == EquipmentSlot.MainHand)
						UpdateExpertise(WeaponAttackType.BaseAttack);
					else if (slot == EquipmentSlot.OffHand)
						UpdateExpertise(WeaponAttackType.OffAttack);

					// equipment visual show
					SetVisibleItemSlot(slot, null);
				}

				_items[slot] = null;
			}
			else if ((pBag = GetBagByPos(bag)) != null)
			{
				pBag.RemoveItem(slot, update);
			}

			// Delete rolled money / loot from db.
			// MUST be done before RemoveFromWorld() or GetTemplate() fails
			if (pProto.HasFlag(ItemFlags.HasLoot))
				Global.LootItemStorage.RemoveStoredLootForContainer(pItem.GUID.Counter);

			ItemRemovedQuestCheck(pItem.Entry, pItem.Count);

			if (IsInWorld && update)
			{
				pItem.RemoveFromWorld();
				pItem.DestroyForPlayer(this);
			}

			//pItem.SetOwnerGUID(ObjectGuid.Empty);
			pItem.SetContainedIn(ObjectGuid.Empty);
			pItem.SetSlot(ItemConst.NullSlot);
			pItem.SetState(ItemUpdateState.Removed, this);

			if (pProto.InventoryType != InventoryType.NonEquip)
				UpdateAverageItemLevelTotal();

			if (bag == InventorySlots.Bag0)
				UpdateAverageItemLevelEquipped();
		}
	}

	public uint DestroyItemCount(uint itemEntry, uint count, bool update, bool unequip_check = true)
	{
		Log.Logger.Debug("STORAGE: DestroyItemCount item = {0}, count = {1}", itemEntry, count);
		uint remcount = 0;

		// in inventory
		var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

		for (var i = InventorySlots.ItemStart; i < inventoryEnd; ++i)
		{
			var item = GetItemByPos(InventorySlots.Bag0, i);

			if (item != null)
				if (item.Entry == itemEntry && !item.IsInTrade)
				{
					if (item.Count + remcount <= count)
					{
						// all items in inventory can unequipped
						remcount += item.Count;
						DestroyItem(InventorySlots.Bag0, i, update);

						if (remcount >= count)
							return remcount;
					}
					else
					{
						item.SetCount(item.Count - count + remcount);
						ItemRemovedQuestCheck(item.Entry, count - remcount);

						if (IsInWorld && update)
							item.SendUpdateToPlayer(this);

						item.SetState(ItemUpdateState.Changed, this);

						return count;
					}
				}
		}

		// in inventory bags
		for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
		{
			var bag = GetBagByPos(i);

			if (bag != null)
				for (byte j = 0; j < bag.GetBagSize(); j++)
				{
					var item = bag.GetItemByPos(j);

					if (item != null)
						if (item.Entry == itemEntry && !item.IsInTrade)
						{
							// all items in bags can be unequipped
							if (item.Count + remcount <= count)
							{
								remcount += item.Count;
								DestroyItem(i, j, update);

								if (remcount >= count)
									return remcount;
							}
							else
							{
								item.SetCount(item.Count - count + remcount);
								ItemRemovedQuestCheck(item.Entry, count - remcount);

								if (IsInWorld && update)
									item.SendUpdateToPlayer(this);

								item.SetState(ItemUpdateState.Changed, this);

								return count;
							}
						}
				}
		}

		// in equipment and bag list
		for (var i = EquipmentSlot.Start; i < InventorySlots.BagEnd; i++)
		{
			var item = GetItemByPos(InventorySlots.Bag0, i);

			if (item != null)
				if (item.Entry == itemEntry && !item.IsInTrade)
				{
					if (item.Count + remcount <= count)
					{
						if (!unequip_check || CanUnequipItem((ushort)(InventorySlots.Bag0 << 8 | i), false) == InventoryResult.Ok)
						{
							remcount += item.Count;
							DestroyItem(InventorySlots.Bag0, i, update);

							if (remcount >= count)
								return remcount;
						}
					}
					else
					{
						item.SetCount(item.Count - count + remcount);
						ItemRemovedQuestCheck(item.Entry, count - remcount);

						if (IsInWorld && update)
							item.SendUpdateToPlayer(this);

						item.SetState(ItemUpdateState.Changed, this);

						return count;
					}
				}
		}

		// in bank
		for (var i = InventorySlots.BankItemStart; i < InventorySlots.BankItemEnd; i++)
		{
			var item = GetItemByPos(InventorySlots.Bag0, i);

			if (item != null)
				if (item.Entry == itemEntry && !item.IsInTrade)
				{
					if (item.Count + remcount <= count)
					{
						remcount += item.Count;
						DestroyItem(InventorySlots.Bag0, i, update);

						if (remcount >= count)
							return remcount;
					}
					else
					{
						item.SetCount(item.Count - count + remcount);
						ItemRemovedQuestCheck(item.Entry, count - remcount);

						if (IsInWorld && update)
							item.SendUpdateToPlayer(this);

						item.SetState(ItemUpdateState.Changed, this);

						return count;
					}
				}
		}

		// in bank bags
		for (var i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; i++)
		{
			var bag = GetBagByPos(i);

			if (bag != null)
				for (byte j = 0; j < bag.GetBagSize(); j++)
				{
					var item = bag.GetItemByPos(j);

					if (item != null)
						if (item.Entry == itemEntry && !item.IsInTrade)
						{
							// all items in bags can be unequipped
							if (item.Count + remcount <= count)
							{
								remcount += item.Count;
								DestroyItem(i, j, update);

								if (remcount >= count)
									return remcount;
							}
							else
							{
								item.SetCount(item.Count - count + remcount);
								ItemRemovedQuestCheck(item.Entry, count - remcount);

								if (IsInWorld && update)
									item.SendUpdateToPlayer(this);

								item.SetState(ItemUpdateState.Changed, this);

								return count;
							}
						}
				}
		}

		// in bank bag list
		for (var i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; i++)
		{
			var item = GetItemByPos(InventorySlots.Bag0, i);

			if (item)
				if (item.Entry == itemEntry && !item.IsInTrade)
				{
					if (item.Count + remcount <= count)
					{
						if (!unequip_check || CanUnequipItem((ushort)(InventorySlots.Bag0 << 8 | i), false) == InventoryResult.Ok)
						{
							remcount += item.Count;
							DestroyItem(InventorySlots.Bag0, i, update);

							if (remcount >= count)
								return remcount;
						}
					}
					else
					{
						item.SetCount(item.Count - count + remcount);
						ItemRemovedQuestCheck(item.Entry, count - remcount);

						if (IsInWorld && update)
							item.SendUpdateToPlayer(this);

						item.SetState(ItemUpdateState.Changed, this);

						return count;
					}
				}
		}

		for (var i = InventorySlots.ReagentStart; i < InventorySlots.ReagentEnd; ++i)
		{
			var item = GetItemByPos(InventorySlots.Bag0, i);

			if (item)
				if (item.Entry == itemEntry && !item.IsInTrade)
				{
					if (item.Count + remcount <= count)
					{
						// all keys can be unequipped
						remcount += item.Count;
						DestroyItem(InventorySlots.Bag0, i, update);

						if (remcount >= count)
							return remcount;
					}
					else
					{
						item.SetCount(item.Count - count + remcount);
						ItemRemovedQuestCheck(item.Entry, count - remcount);

						if (IsInWorld && update)
							item.SendUpdateToPlayer(this);

						item.SetState(ItemUpdateState.Changed, this);

						return count;
					}
				}
		}

		for (var i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
		{
			var item = GetItemByPos(InventorySlots.Bag0, i);

			if (item)
				if (item.Entry == itemEntry && !item.IsInTrade)
				{
					if (item.Count + remcount <= count)
					{
						// all keys can be unequipped
						remcount += item.Count;
						DestroyItem(InventorySlots.Bag0, i, update);

						if (remcount >= count)
							return remcount;
					}
					else
					{
						item.SetCount(item.Count - count + remcount);
						ItemRemovedQuestCheck(item.Entry, count - remcount);

						if (IsInWorld && update)
							item.SendUpdateToPlayer(this);

						item.SetState(ItemUpdateState.Changed, this);

						return count;
					}
				}
		}

		return remcount;
	}

	public void DestroyItemCount(Item pItem, ref uint count, bool update)
	{
		if (pItem == null)
			return;

		Log.Logger.Debug("STORAGE: DestroyItemCount item (GUID: {0}, Entry: {1}) count = {2}", pItem.GUID.ToString(), pItem.Entry, count);

		if (pItem.Count <= count)
		{
			count -= pItem.Count;

			DestroyItem(pItem.BagSlot, pItem.Slot, update);
		}
		else
		{
			ItemRemovedQuestCheck(pItem.Entry, count);
			pItem.SetCount(pItem.Count - count);
			count = 0;

			if (IsInWorld && update)
				pItem.SendUpdateToPlayer(this);

			pItem.SetState(ItemUpdateState.Changed, this);
		}
	}

	public void AutoStoreLoot(uint loot_id, LootStore store, ItemContext context = 0, bool broadcast = false, bool createdByPlayer = false)
	{
		AutoStoreLoot(ItemConst.NullBag, ItemConst.NullSlot, loot_id, store, context, broadcast);
	}

	public byte GetInventorySlotCount()
	{
		return ActivePlayerData.NumBackpackSlots;
	}

	public void SetInventorySlotCount(byte slots)
	{
		//ASSERT(slots <= (INVENTORY_SLOT_ITEM_END - INVENTORY_SLOT_ITEM_START));

		if (slots < GetInventorySlotCount())
		{
			List<Item> unstorableItems = new();

			for (var slot = (byte)(InventorySlots.ItemStart + slots); slot < InventorySlots.ItemEnd; ++slot)
			{
				var unstorableItem = GetItemByPos(InventorySlots.Bag0, slot);

				if (unstorableItem)
					unstorableItems.Add(unstorableItem);
			}

			if (!unstorableItems.Empty())
			{
				var fullBatches = unstorableItems.Count / SharedConst.MaxMailItems;
				var remainder = unstorableItems.Count % SharedConst.MaxMailItems;
				SQLTransaction trans = new();

				var sendItemsBatch = new Action<int, int>((batchNumber, batchSize) =>
				{
					MailDraft draft = new(Global.ObjectMgr.GetCypherString(CypherStrings.NotEquippedItem), "There were problems with equipping item(s).");

					for (var j = 0; j < batchSize; ++j)
						draft.AddItem(unstorableItems[batchNumber * SharedConst.MaxMailItems + j]);

					draft.SendMailTo(trans, this, new MailSender(this, MailStationery.Gm), MailCheckMask.Copied);
				});

				for (var batch = 0; batch < fullBatches; ++batch)
					sendItemsBatch(batch, SharedConst.MaxMailItems);

				if (remainder != 0)
					sendItemsBatch(fullBatches, remainder);

				DB.Characters.CommitTransaction(trans);

				SendPacket(new InventoryFullOverflow());
			}
		}

		SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.NumBackpackSlots), slots);
	}

	public byte GetBankBagSlotCount()
	{
		return ActivePlayerData.NumBankSlots;
	}

	public void SetBankBagSlotCount(byte count)
	{
		SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.NumBankSlots), count);
	}

	//Loot
	public ObjectGuid GetLootGUID()
	{
		return PlayerData.LootTargetGUID;
	}

	public void SetLootGUID(ObjectGuid guid)
	{
		SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.LootTargetGUID), guid);
	}

	public void StoreLootItem(ObjectGuid lootWorldObjectGuid, byte lootSlot, Loot.Loot loot, AELootResult aeResult = null)
	{
		var item = loot.LootItemInSlot(lootSlot, this, out var ffaItem);

		if (item == null || item.is_looted)
		{
			SendEquipError(InventoryResult.LootGone);

			return;
		}

		if (!item.HasAllowedLooter(GUID))
		{
			SendLootReleaseAll();

			return;
		}

		if (item.is_blocked)
		{
			SendLootReleaseAll();

			return;
		}

		// dont allow protected item to be looted by someone else
		if (!item.rollWinnerGUID.IsEmpty && item.rollWinnerGUID != GUID)
		{
			SendLootReleaseAll();

			return;
		}

		List<ItemPosCount> dest = new();
		var msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item.itemid, item.count);

		if (msg == InventoryResult.Ok)
		{
			var newitem = StoreNewItem(dest, item.itemid, true, item.randomBonusListId, item.GetAllowedLooters(), item.context, item.BonusListIDs);

			if (ffaItem != null)
			{
				//freeforall case, notify only one player of the removal
				ffaItem.is_looted = true;
				SendNotifyLootItemRemoved(loot.GetGUID(), loot.GetOwnerGUID(), lootSlot);
			}
			else //not freeforall, notify everyone
			{
				loot.NotifyItemRemoved(lootSlot, Map);
			}

			//if only one person is supposed to loot the item, then set it to looted
			if (!item.freeforall)
				item.is_looted = true;

			--loot.unlootedCount;

			if (Global.ObjectMgr.GetItemTemplate(item.itemid) != null)
				if (newitem.Quality > ItemQuality.Epic || (newitem.Quality == ItemQuality.Epic && newitem.GetItemLevel(this) >= GuildConst.MinNewsItemLevel))
				{
					var guild = Guild;

					if (guild)
						guild.AddGuildNews(GuildNews.ItemLooted, GUID, 0, item.itemid);
				}

			// if aeLooting then we must delay sending out item so that it appears properly stacked in chat
			if (aeResult == null)
			{
				SendNewItem(newitem, item.count, false, false, true, loot.GetDungeonEncounterId());
				UpdateCriteria(CriteriaType.LootItem, item.itemid, item.count);
				UpdateCriteria(CriteriaType.GetLootByType, item.itemid, item.count, (uint)SharedConst.GetLootTypeForClient(loot.loot_type));
				UpdateCriteria(CriteriaType.LootAnyItem, item.itemid, item.count);
			}
			else
			{
				aeResult.Add(newitem, item.count, SharedConst.GetLootTypeForClient(loot.loot_type), loot.GetDungeonEncounterId());
			}

			// LootItem is being removed (looted) from the container, delete it from the DB.
			if (loot.loot_type == LootType.Item)
				Global.LootItemStorage.RemoveStoredLootItemForContainer(lootWorldObjectGuid.Counter, item.itemid, item.count, item.LootListId);

			ApplyItemLootedSpell(newitem, true);
		}
		else
		{
			SendEquipError(msg, null, null, item.itemid);
		}
	}

	public Dictionary<ObjectGuid, Loot.Loot> GetAELootView()
	{
		return _aeLootView;
	}

	/// <summary>
	///  if in a Battleground a player dies, and an enemy removes the insignia, the player's bones is lootable
	///  Called by remove insignia spell effect
	/// </summary>
	/// <param name="looterPlr"> </param>
	public void RemovedInsignia(Player looterPlr)
	{
		// If player is not in battleground and not in worldpvpzone
		if (BattlegroundId == 0 && !IsInWorldPvpZone)
			return;

		// If not released spirit, do it !
		if (_deathTimer > 0)
		{
			_deathTimer = 0;
			BuildPlayerRepop();
			RepopAtGraveyard();
		}

		_corpseLocation = new WorldLocation();

		// We have to convert player corpse to bones, not to be able to resurrect there
		// SpawnCorpseBones isn't handy, 'cos it saves player while he in BG
		var bones = Map.ConvertCorpseToBones(GUID, true);

		if (!bones)
			return;

		// Now we must make bones lootable, and send player loot
		bones.SetCorpseDynamicFlag(CorpseDynFlags.Lootable);

		bones.Loot = new Loot.Loot(Map, bones.GUID, LootType.Insignia, looterPlr.Group);

		// For AV Achievement
		var bg = Battleground;

		if (bg != null)
		{
			if (bg.GetTypeID(true) == BattlegroundTypeId.AV)
				bones.Loot.FillLoot(1, LootStorage.Creature, this, true);
		}
		// For wintergrasp Quests
		else if (Zone == (uint)AreaId.Wintergrasp)
		{
			bones.Loot.FillLoot(1, LootStorage.Creature, this, true);
		}

		// It may need a better formula
		// Now it works like this: lvl10: ~6copper, lvl70: ~9silver
		bones.Loot.gold = (uint)(RandomHelper.URand(50, 150) * 0.016f * Math.Pow((float)Level / 5.76f, 2.5f) * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney));
		bones.LootRecipient = looterPlr;
		looterPlr.SendLoot(bones.Loot);
	}

	public void SendLootRelease(ObjectGuid guid)
	{
		LootReleaseResponse packet = new();

		{
			LootObj = guid,
			Owner = GUID
		}

		p
		dPacket(packet);
	}

	public void SendLootReleaseAll()
	{
		SendPacket(new LootReleaseAll());
	}

	public void SendLoot(Loot.Loot loot, bool aeLooting = false)
	{
		if (!GetLootGUID().IsEmpty && !aeLooting)
			_session.DoLootReleaseAll();

		Log.Logger.Debug($"Player::SendLoot: Player: '{GetName()}' ({GUID}), Loot: {loot.GetOwnerGUID()}");

		if (!loot.GetOwnerGUID().IsItem && !aeLooting)
			SetLootGUID(loot.GetOwnerGUID());

		LootResponse packet = new();

		{
			Owner = loot.GetOwnerGUID(),
			LootObj = loot.GetGUID(),
			LootMethod = loot.GetLootMethod(),
			AcquireReason = (byte)SharedConst.GetLootTypeForClient(loot.loot_type),
			Acquired = true, // false == No Loot (this too^^)
			AELooting = aeLooting
		}

		p
		t.BuildLootResponse(packet, this);
		SendPacket(packet);

		// add 'this' player as one of the players that are looting 'loot'
		loot.OnLootOpened(Map, GUID);
		_aeLootView[loot.GetGUID()] = loot;

		if (loot.loot_type == LootType.Corpse && !loot.GetOwnerGUID().IsItem)
			SetUnitFlag(UnitFlags.Looting);
	}

	public void SendLootError(ObjectGuid lootObj, ObjectGuid owner, LootError error)
	{
		LootResponse packet = new();

		{
			LootObj = lootObj,
			Owner = owner,
			Acquired = false,
			FailureReason = error
		}

		p
		dPacket(packet);
	}

	public void SendNotifyLootMoneyRemoved(ObjectGuid lootObj)
	{
		CoinRemoved packet = new();

		{
			LootObj = lootObj
		}

		S
		dPacket(packet);
	}

	public void SendNotifyLootItemRemoved(ObjectGuid lootObj, ObjectGuid owner, byte lootListId)
	{
		LootRemoved packet = new();

		{
			LootObj = lootObj,
			Owner = owner,
			LootListID = lootListId
		}

		p
		dPacket(packet);
	}

	public void SetEquipmentSet(EquipmentSetInfo.EquipmentSetData newEqSet)
	{
		if (newEqSet.Guid != 0)
		{
			// something wrong...
			var equipmentSetInfo = _equipmentSets.LookupByKey(newEqSet.Guid);

			if (equipmentSetInfo == null || equipmentSetInfo.Data.Guid != newEqSet.Guid)
			{
				Log.Logger.Error("Player {0} tried to save equipment set {1} (index: {2}), but that equipment set not found!", GetName(), newEqSet.Guid, newEqSet.SetId);

				return;
			}
		}

		var setGuid = (newEqSet.Guid != 0) ? newEqSet.Guid : Global.ObjectMgr.GenerateEquipmentSetGuid();

		if (!_equipmentSets.ContainsKey(setGuid))
			_equipmentSets[setGuid] = new EquipmentSetInfo();

		var eqSlot = _equipmentSets[setGuid];
		eqSlot.Data = newEqSet;

		if (eqSlot.Data.Guid == 0)
		{
			eqSlot.Data.Guid = setGuid;

			EquipmentSetID data = new();

			{
				GUID = eqSlot.Data.Guid,
				Type = (int)eqSlot.Data.Type,
				SetID = eqSlot.Data.SetId
			}

			dPacket(data);
		}

		eqSlot.State = eqSlot.State == EquipmentSetUpdateState.New ? EquipmentSetUpdateState.New : EquipmentSetUpdateState.Changed;
	}

	public void DeleteEquipmentSet(ulong id)
	{
		foreach (var pair in _equipmentSets)
			if (pair.Value.Data.Guid == id)
			{
				if (pair.Value.State == EquipmentSetUpdateState.New)
					_equipmentSets.Remove(pair.Key);
				else
					pair.Value.State = EquipmentSetUpdateState.Deleted;

				break;
			}
	}

	//Void Storage
	public bool IsVoidStorageUnlocked()
	{
		return HasPlayerFlag(PlayerFlags.VoidUnlocked);
	}

	public void UnlockVoidStorage()
	{
		SetPlayerFlag(PlayerFlags.VoidUnlocked);
	}

	public void LockVoidStorage()
	{
		RemovePlayerFlag(PlayerFlags.VoidUnlocked);
	}

	public byte GetNextVoidStorageFreeSlot()
	{
		for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
			if (_voidStorageItems[i] == null) // unused item
				return i;

		return SharedConst.VoidStorageMaxSlot;
	}

	public byte GetNumOfVoidStorageFreeSlots()
	{
		byte count = 0;

		for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
			if (_voidStorageItems[i] == null)
				count++;

		return count;
	}

	public byte AddVoidStorageItem(VoidStorageItem item)
	{
		var slot = GetNextVoidStorageFreeSlot();

		if (slot >= SharedConst.VoidStorageMaxSlot)
		{
			Session.SendVoidStorageTransferResult(VoidTransferError.Full);

			return 255;
		}

		_voidStorageItems[slot] = item;

		return slot;
	}

	public void DeleteVoidStorageItem(byte slot)
	{
		if (slot >= SharedConst.VoidStorageMaxSlot)
		{
			Session.SendVoidStorageTransferResult(VoidTransferError.InternalError1);

			return;
		}

		_voidStorageItems[slot] = null;
	}

	public bool SwapVoidStorageItem(byte oldSlot, byte newSlot)
	{
		if (oldSlot >= SharedConst.VoidStorageMaxSlot || newSlot >= SharedConst.VoidStorageMaxSlot || oldSlot == newSlot)
			return false;

		_voidStorageItems.Swap(newSlot, oldSlot);

		return true;
	}

	public VoidStorageItem GetVoidStorageItem(byte slot)
	{
		if (slot >= SharedConst.VoidStorageMaxSlot)
		{
			Session.SendVoidStorageTransferResult(VoidTransferError.InternalError1);

			return null;
		}

		return _voidStorageItems[slot];
	}

	public VoidStorageItem GetVoidStorageItem(ulong id, out byte slot)
	{
		slot = 0;

		for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
			if (_voidStorageItems[i] != null && _voidStorageItems[i].ItemId == id)
			{
				slot = i;

				return _voidStorageItems[i];
			}

		return null;
	}

	public bool ForEachItem(ItemSearchLocation location, Func<Item, bool> callback)
	{
		if (location.HasAnyFlag(ItemSearchLocation.Equipment))
		{
			for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; i++)
			{
				var item = GetItemByPos(InventorySlots.Bag0, i);

				if (item != null)
					if (!callback(item))
						return false;
			}

			for (var i = ProfessionSlots.Start; i < ProfessionSlots.End; ++i)
			{
				var pItem = GetItemByPos(InventorySlots.Bag0, i);

				if (pItem != null)
					if (!callback(pItem))
						return false;
			}
		}

		if (location.HasAnyFlag(ItemSearchLocation.Inventory))
		{
			var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

			for (var i = InventorySlots.ItemStart; i < inventoryEnd; i++)
			{
				var item = GetItemByPos(InventorySlots.Bag0, i);

				if (item != null)
					if (!callback(item))
						return false;
			}

			for (var i = InventorySlots.ChildEquipmentStart; i < InventorySlots.ChildEquipmentEnd; ++i)
			{
				var item = GetItemByPos(InventorySlots.Bag0, i);

				if (item != null)
					if (!callback(item))
						return false;
			}

			for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
			{
				var bag = GetBagByPos(i);

				if (bag != null)
					for (byte j = 0; j < bag.GetBagSize(); ++j)
					{
						var pItem = bag.GetItemByPos(j);

						if (pItem != null)
							if (!callback(pItem))
								return false;
					}
			}
		}

		if (location.HasAnyFlag(ItemSearchLocation.Bank))
		{
			for (var i = InventorySlots.BankItemStart; i < InventorySlots.BankItemEnd; ++i)
			{
				var item = GetItemByPos(InventorySlots.Bag0, i);

				if (item != null)
					if (!callback(item))
						return false;
			}

			for (var i = InventorySlots.BankBagStart; i < InventorySlots.BankBagEnd; ++i)
			{
				var bag = GetBagByPos(i);

				if (bag != null)
					for (byte j = 0; j < bag.GetBagSize(); ++j)
					{
						var pItem = bag.GetItemByPos(j);

						if (pItem != null)
							if (!callback(pItem))
								return false;
					}
			}
		}

		if (location.HasAnyFlag(ItemSearchLocation.ReagentBank))
		{
			for (var i = InventorySlots.ReagentBagStart; i < InventorySlots.ReagentBagEnd; ++i)
			{
				var bag = GetBagByPos(i);

				if (bag != null)
					for (byte j = 0; j < bag.GetBagSize(); ++j)
					{
						var pItem = bag.GetItemByPos(j);

						if (pItem != null)
							if (!callback(pItem))
								return false;
					}
			}

			for (var i = InventorySlots.ReagentStart; i < InventorySlots.ReagentEnd; ++i)
			{
				var item = GetItemByPos(InventorySlots.Bag0, i);

				if (item != null)
					if (!callback(item))
						return false;
			}
		}

		return true;
	}

	public void UpdateAverageItemLevelTotal()
	{
		var bestItemLevels = new (InventoryType inventoryType, uint itemLevel, ObjectGuid guid)[EquipmentSlot.End];
		float sum = 0;

		ForEachItem(ItemSearchLocation.Everywhere,
					item =>
					{
						var itemTemplate = item.Template;

						if (itemTemplate != null && itemTemplate.InventoryType < InventoryType.ProfessionTool)
						{
							if (item.IsEquipped)
							{
								var itemLevel = item.GetItemLevel(this);
								var inventoryType = itemTemplate.InventoryType;
								ref var slotData = ref bestItemLevels[item.Slot];

								if (itemLevel > slotData.Item2)
								{
									sum += itemLevel - slotData.Item2;
									slotData = (inventoryType, itemLevel, item.GUID);
								}
							}
							else if (CanEquipItem(ItemConst.NullSlot, out var dest, item, true, false) == InventoryResult.Ok)
							{
								var itemLevel = item.GetItemLevel(this);
								var inventoryType = itemTemplate.InventoryType;

								ForEachEquipmentSlot(inventoryType,
													CanDualWield,
													_canTitanGrip,
													(slot, checkDuplicateGuid) =>
													{
														if (checkDuplicateGuid)
															foreach (var slotData1 in bestItemLevels)
																if (slotData1.guid == item.GUID)
																	return;

														ref var slotData = ref bestItemLevels[slot];

														if (itemLevel > slotData.itemLevel)
														{
															sum += itemLevel - slotData.itemLevel;
															slotData = (inventoryType, itemLevel, item.GUID);
														}
													});
							}
						}

						return true;
					});

		// If main hand is a 2h weapon, count it twice
		var mainHand = bestItemLevels[EquipmentSlot.MainHand];

		if (!_canTitanGrip && mainHand.inventoryType == InventoryType.Weapon2Hand)
			sum += mainHand.itemLevel;

		sum /= 16.0f;
		SetAverageItemLevelTotal(sum);
	}

	public void UpdateAverageItemLevelEquipped()
	{
		float totalItemLevel = 0;

		for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; i++)
		{
			var item = GetItemByPos(InventorySlots.Bag0, i);

			if (item != null)
			{
				var itemLevel = item.GetItemLevel(this);
				totalItemLevel += itemLevel;

				if (!_canTitanGrip && i == EquipmentSlot.MainHand && item.Template.InventoryType == InventoryType.Weapon2Hand) // 2h weapon counts twice
					totalItemLevel += itemLevel;
			}
		}

		totalItemLevel /= 16.0f;
		SetAverageItemLevelEquipped(totalItemLevel);
	}

	//Refund
	void AddRefundReference(ObjectGuid it)
	{
		_refundableItems.Add(it);
	}

	//Trade 
	void AddTradeableItem(Item item)
	{
		_itemSoulboundTradeable.Add(item.GUID);
	}

	void UpdateSoulboundTradeItems()
	{
		// also checks for garbage data
		foreach (var guid in _itemSoulboundTradeable.ToList())
		{
			var item = GetItemByGuid(guid);

			if (!item || item.OwnerGUID != GUID || item.CheckSoulboundTradeExpire())
				_itemSoulboundTradeable.Remove(guid);
		}
	}

	InventoryResult CanStoreItem(byte bag, byte slot, List<ItemPosCount> dest, uint entry, uint count, Item pItem, bool swap)
	{
		return CanStoreItem(bag, slot, dest, entry, count, pItem, swap, out _);
	}

	InventoryResult CanStoreItem(byte bag, byte slot, List<ItemPosCount> dest, uint entry, uint count, Item pItem, bool swap, out uint no_space_count)
	{
		no_space_count = 0;
		Log.Logger.Debug("STORAGE: CanStoreItem bag = {0}, slot = {1}, item = {2}, count = {3}", bag, slot, entry, count);

		var pProto = Global.ObjectMgr.GetItemTemplate(entry);

		if (pProto == null)
		{
			no_space_count = count;

			return swap ? InventoryResult.CantSwap : InventoryResult.ItemNotFound;
		}

		if (pItem != null)
		{
			// item used
			if (pItem.LootGenerated)
			{
				no_space_count = count;

				return InventoryResult.LootGone;
			}

			if (pItem.IsBindedNotWith(this))
			{
				no_space_count = count;

				return InventoryResult.NotOwner;
			}
		}

		// check count of items (skip for auto move for same player from bank)
		uint no_similar_count = 0; // can't store this amount similar items
		var res = CanTakeMoreSimilarItems(entry, count, pItem, ref no_similar_count);

		if (res != InventoryResult.Ok)
		{
			if (count == no_similar_count)
			{
				no_space_count = no_similar_count;

				return res;
			}

			count -= no_similar_count;
		}

		// in specific slot
		if (bag != ItemConst.NullBag && slot != ItemConst.NullSlot)
		{
			res = CanStoreItem_InSpecificSlot(bag, slot, dest, pProto, ref count, swap, pItem);

			if (res != InventoryResult.Ok)
			{
				no_space_count = count + no_similar_count;

				return res;
			}

			if (count == 0)
			{
				if (no_similar_count == 0)
					return InventoryResult.Ok;

				no_space_count = count + no_similar_count;

				return InventoryResult.ItemMaxCount;
			}
		}

		// not specific slot or have space for partly store only in specific slot
		var inventoryEnd = (byte)(InventorySlots.ItemStart + GetInventorySlotCount());

		// in specific bag
		if (bag != ItemConst.NullBag)
		{
			// search stack in bag for merge to
			if (pProto.MaxStackSize != 1)
			{
				if (bag == InventorySlots.Bag0) // inventory
				{
					res = CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, dest, pProto, ref count, true, pItem, bag, slot);

					if (res != InventoryResult.Ok)
					{
						no_space_count = count + no_similar_count;

						return res;
					}

					if (count == 0)
					{
						if (no_similar_count == 0)
							return InventoryResult.Ok;

						no_space_count = count + no_similar_count;

						return InventoryResult.ItemMaxCount;
					}

					res = CanStoreItem_InInventorySlots(InventorySlots.ItemStart, inventoryEnd, dest, pProto, ref count, true, pItem, bag, slot);

					if (res != InventoryResult.Ok)
					{
						no_space_count = count + no_similar_count;

						return res;
					}

					if (count == 0)
					{
						if (no_similar_count == 0)
							return InventoryResult.Ok;


						no_space_count = count + no_similar_count;

						return InventoryResult.ItemMaxCount;
					}
				}
				else // equipped bag
				{
					// we need check 2 time (specialized/non_specialized), use NULL_BAG to prevent skipping bag
					res = CanStoreItem_InBag(bag, dest, pProto, ref count, true, false, pItem, ItemConst.NullBag, slot);

					if (res != InventoryResult.Ok)
						res = CanStoreItem_InBag(bag, dest, pProto, ref count, true, true, pItem, ItemConst.NullBag, slot);

					if (res != InventoryResult.Ok)
					{
						no_space_count = count + no_similar_count;

						return res;
					}

					if (count == 0)
					{
						if (no_similar_count == 0)
							return InventoryResult.Ok;

						no_space_count = count + no_similar_count;

						return InventoryResult.ItemMaxCount;
					}
				}
			}

			// search free slot in bag for place to
			if (bag == InventorySlots.Bag0) // inventory
			{
				if (pItem && pItem.HasItemFlag(ItemFieldFlags.Child))
				{
					res = CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, dest, pProto, ref count, false, pItem, bag, slot);

					if (res != InventoryResult.Ok)
					{
						no_space_count = count + no_similar_count;

						return res;
					}

					if (count == 0)
					{
						if (no_similar_count == 0)
							return InventoryResult.Ok;

						no_space_count = count + no_similar_count;

						return InventoryResult.ItemMaxCount;
					}
				}

				res = CanStoreItem_InInventorySlots(InventorySlots.ItemStart, inventoryEnd, dest, pProto, ref count, false, pItem, bag, slot);

				if (res != InventoryResult.Ok)
				{
					no_space_count = count + no_similar_count;

					return res;
				}

				if (count == 0)
				{
					if (no_similar_count == 0)
						return InventoryResult.Ok;

					no_space_count = count + no_similar_count;

					return InventoryResult.ItemMaxCount;
				}
			}
			else // equipped bag
			{
				res = CanStoreItem_InBag(bag, dest, pProto, ref count, false, false, pItem, ItemConst.NullBag, slot);

				if (res != InventoryResult.Ok)
					res = CanStoreItem_InBag(bag, dest, pProto, ref count, false, true, pItem, ItemConst.NullBag, slot);

				if (res != InventoryResult.Ok)
				{
					no_space_count = count + no_similar_count;

					return res;
				}

				if (count == 0)
				{
					if (no_similar_count == 0)
						return InventoryResult.Ok;

					no_space_count = count + no_similar_count;

					return InventoryResult.ItemMaxCount;
				}
			}
		}

		// not specific bag or have space for partly store only in specific bag

		// search stack for merge to
		if (pProto.MaxStackSize != 1)
		{
			res = CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, dest, pProto, ref count, true, pItem, bag, slot);

			if (res != InventoryResult.Ok)
			{
				no_space_count = count + no_similar_count;

				return res;
			}

			if (count == 0)
			{
				if (no_similar_count == 0)
					return InventoryResult.Ok;

				no_space_count = count + no_similar_count;

				return InventoryResult.ItemMaxCount;
			}

			res = CanStoreItem_InInventorySlots(InventorySlots.ItemStart, inventoryEnd, dest, pProto, ref count, true, pItem, bag, slot);

			if (res != InventoryResult.Ok)
			{
				no_space_count = count + no_similar_count;

				return res;
			}

			if (count == 0)
			{
				if (no_similar_count == 0)
					return InventoryResult.Ok;

				no_space_count = count + no_similar_count;

				return InventoryResult.ItemMaxCount;
			}

			if (pProto.BagFamily != 0)
				for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
				{
					res = CanStoreItem_InBag(i, dest, pProto, ref count, true, false, pItem, bag, slot);

					if (res != InventoryResult.Ok)
						continue;

					if (count == 0)
					{
						if (no_similar_count == 0)
							return InventoryResult.Ok;

						no_space_count = count + no_similar_count;

						return InventoryResult.ItemMaxCount;
					}
				}

			for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
			{
				res = CanStoreItem_InBag(i, dest, pProto, ref count, true, true, pItem, bag, slot);

				if (res != InventoryResult.Ok)
					continue;

				if (count == 0)
				{
					if (no_similar_count == 0)
						return InventoryResult.Ok;

					no_space_count = count + no_similar_count;

					return InventoryResult.ItemMaxCount;
				}
			}
		}

		// search free slot - special bag case
		if (pProto.BagFamily != 0)
			for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
			{
				res = CanStoreItem_InBag(i, dest, pProto, ref count, false, false, pItem, bag, slot);

				if (res != InventoryResult.Ok)
					continue;

				if (count == 0)
				{
					if (no_similar_count == 0)
						return InventoryResult.Ok;

					no_space_count = count + no_similar_count;

					return InventoryResult.ItemMaxCount;
				}
			}

		if (pItem != null && pItem.IsNotEmptyBag)
			return InventoryResult.BagInBag;

		if (pItem && pItem.HasItemFlag(ItemFieldFlags.Child))
		{
			res = CanStoreItem_InInventorySlots(InventorySlots.ChildEquipmentStart, InventorySlots.ChildEquipmentEnd, dest, pProto, ref count, false, pItem, bag, slot);

			if (res != InventoryResult.Ok)
			{
				no_space_count = count + no_similar_count;

				return res;
			}

			if (count == 0)
			{
				if (no_similar_count == 0)
					return InventoryResult.Ok;

				no_space_count = count + no_similar_count;

				return InventoryResult.ItemMaxCount;
			}
		}

		// search free slot
		var searchSlotStart = InventorySlots.ItemStart;

		// new bags can be directly equipped
		if (!pItem &&
			pProto.Class == ItemClass.Container &&
			(ItemSubClassContainer)pProto.SubClass == ItemSubClassContainer.Container &&
			(pProto.Bonding == ItemBondingType.None || pProto.Bonding == ItemBondingType.OnAcquire))
			searchSlotStart = InventorySlots.BagStart;

		res = CanStoreItem_InInventorySlots(searchSlotStart, inventoryEnd, dest, pProto, ref count, false, pItem, bag, slot);

		if (res != InventoryResult.Ok)
		{
			no_space_count = count + no_similar_count;

			return res;
		}

		if (count == 0)
		{
			if (no_similar_count == 0)
				return InventoryResult.Ok;

			no_space_count = count + no_similar_count;

			return InventoryResult.ItemMaxCount;
		}

		for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
		{
			res = CanStoreItem_InBag(i, dest, pProto, ref count, false, true, pItem, bag, slot);

			if (res != InventoryResult.Ok)
				continue;

			if (count == 0)
			{
				if (no_similar_count == 0)
					return InventoryResult.Ok;

				no_space_count = count + no_similar_count;

				return InventoryResult.ItemMaxCount;
			}
		}

		no_space_count = count + no_similar_count;

		return InventoryResult.InvFull;
	}

	Item _StoreItem(ushort pos, Item pItem, uint count, bool clone, bool update)
	{
		if (pItem == null)
			return null;

		var bag = (byte)(pos >> 8);
		var slot = (byte)(pos & 255);

		Log.Logger.Debug("STORAGE: StoreItem bag = {0}, slot = {1}, item = {2}, count = {3}, guid = {4}", bag, slot, pItem.Entry, count, pItem.GUID.ToString());

		var pItem2 = GetItemByPos(bag, slot);

		if (pItem2 == null)
		{
			if (clone)
				pItem = pItem.CloneItem(count, this);
			else
				pItem.SetCount(count);

			if (pItem == null)
				return null;

			if (pItem.Bonding == ItemBondingType.OnAcquire ||
				pItem.Bonding == ItemBondingType.Quest ||
				(pItem.Bonding == ItemBondingType.OnEquip && IsBagPos(pos)))
				pItem.SetBinding(true);

			var pBag = bag == InventorySlots.Bag0 ? null : GetBagByPos(bag);

			if (pBag == null)
			{
				_items[slot] = pItem;
				SetInvSlot(slot, pItem.GUID);
				pItem.SetContainedIn(GUID);
				pItem.SetOwnerGUID(GUID);

				pItem.SetSlot(slot);
				pItem.SetContainer(null);
			}
			else
			{
				pBag.StoreItem(slot, pItem, update);
			}

			if (IsInWorld && update)
			{
				pItem.AddToWorld();
				pItem.SendUpdateToPlayer(this);
			}

			pItem.SetState(ItemUpdateState.Changed, this);

			if (pBag != null)
				pBag.SetState(ItemUpdateState.Changed, this);

			AddEnchantmentDurations(pItem);
			AddItemDurations(pItem);

			if (bag == InventorySlots.Bag0 || (bag >= InventorySlots.BagStart && bag < InventorySlots.BagEnd))
				ApplyItemObtainSpells(pItem, true);

			return pItem;
		}
		else
		{
			if (pItem2.Bonding == ItemBondingType.OnAcquire ||
				pItem2.Bonding == ItemBondingType.Quest ||
				(pItem2.Bonding == ItemBondingType.OnEquip && IsBagPos(pos)))
				pItem2.SetBinding(true);

			pItem2.SetCount(pItem2.Count + count);

			if (IsInWorld && update)
				pItem2.SendUpdateToPlayer(this);

			if (!clone)
			{
				// delete item (it not in any slot currently)
				if (IsInWorld && update)
				{
					pItem.RemoveFromWorld();
					pItem.DestroyForPlayer(this);
				}

				RemoveEnchantmentDurations(pItem);
				RemoveItemDurations(pItem);

				pItem.SetOwnerGUID(GUID); // prevent error at next SetState in case trade/mail/buy from vendor
				pItem.SetNotRefundable(this);
				pItem.ClearSoulboundTradeable(this);
				RemoveTradeableItem(pItem);
				pItem.SetState(ItemUpdateState.Removed, this);
			}

			AddEnchantmentDurations(pItem2);

			pItem2.SetState(ItemUpdateState.Changed, this);

			if (bag == InventorySlots.Bag0 || (bag >= InventorySlots.BagStart && bag < InventorySlots.BagEnd))
				ApplyItemObtainSpells(pItem2, true);

			return pItem2;
		}
	}

	bool StoreNewItemInBestSlots(uint itemId, uint amount, ItemContext context)
	{
		Log.Logger.Debug("STORAGE: Creating initial item, itemId = {0}, count = {1}", itemId, amount);

		var bonusListIDs = Global.DB2Mgr.GetDefaultItemBonusTree(itemId, context);

		InventoryResult msg;

		// attempt equip by one
		while (amount > 0)
		{
			msg = CanEquipNewItem(ItemConst.NullSlot, out var eDest, itemId, false);

			if (msg != InventoryResult.Ok)
				break;

			var item = EquipNewItem(eDest, itemId, context, true);
			item.SetBonuses(bonusListIDs);
			AutoUnequipOffhandIfNeed();
			--amount;
		}

		if (amount == 0)
			return true; // equipped

		// attempt store
		List<ItemPosCount> sDest = new();
		// store in main bag to simplify second pass (special bags can be not equipped yet at this moment)
		msg = CanStoreNewItem(InventorySlots.Bag0, ItemConst.NullSlot, sDest, itemId, amount);

		if (msg == InventoryResult.Ok)
		{
			StoreNewItem(sDest, itemId, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(itemId), null, context, bonusListIDs);

			return true; // stored
		}

		// item can't be added
		Log.Logger.Error("STORAGE: Can't equip or store initial item {0} for race {1} class {2}, error msg = {3}", itemId, Race, Class, msg);

		return false;
	}

	//Move Item
	InventoryResult CanTakeMoreSimilarItems(Item pItem)
	{
		uint notused = 0;

		return CanTakeMoreSimilarItems(pItem.Entry, pItem.Count, pItem, ref notused);
	}

	InventoryResult CanTakeMoreSimilarItems(Item pItem, ref uint offendingItemId)
	{
		uint notused = 0;

		return CanTakeMoreSimilarItems(pItem.Entry, pItem.Count, pItem, ref notused, ref offendingItemId);
	}

	InventoryResult CanTakeMoreSimilarItems(uint entry, uint count, Item pItem, ref uint no_space_count)
	{
		uint notused = 0;

		return CanTakeMoreSimilarItems(entry, count, pItem, ref no_space_count, ref notused);
	}

	InventoryResult CanTakeMoreSimilarItems(uint entry, uint count, Item pItem, ref uint no_space_count, ref uint offendingItemId)
	{
		var pProto = Global.ObjectMgr.GetItemTemplate(entry);

		if (pProto == null)
		{
			no_space_count = count;

			return InventoryResult.ItemMaxCount;
		}

		if (pItem != null && pItem.LootGenerated)
			return InventoryResult.LootGone;

		// no maximum
		if ((pProto.MaxCount <= 0 && pProto.ItemLimitCategory == 0) || pProto.MaxCount == 2147483647)
			return InventoryResult.Ok;

		if (pProto.MaxCount > 0)
		{
			var curcount = GetItemCount(pProto.Id, true, pItem);

			if (curcount + count > pProto.MaxCount)
			{
				no_space_count = count + curcount - pProto.MaxCount;

				return InventoryResult.ItemMaxCount;
			}
		}

		// check unique-equipped limit
		if (pProto.ItemLimitCategory != 0)
		{
			var limitEntry = CliDB.ItemLimitCategoryStorage.LookupByKey(pProto.ItemLimitCategory);

			if (limitEntry == null)
			{
				no_space_count = count;

				return InventoryResult.NotEquippable;
			}

			if (limitEntry.Flags == 0)
			{
				var limitQuantity = GetItemLimitCategoryQuantity(limitEntry);
				var curcount = GetItemCountWithLimitCategory(pProto.ItemLimitCategory, pItem);

				if (curcount + count > limitQuantity)
				{
					no_space_count = count + curcount - limitQuantity;
					offendingItemId = pProto.Id;

					return InventoryResult.ItemMaxLimitCategoryCountExceededIs;
				}
			}
		}

		return InventoryResult.Ok;
	}

	//Equip/Unequip Item
	InventoryResult CanUnequipItems(uint item, uint count)
	{
		var res = InventoryResult.Ok;

		uint tempcount = 0;

		var result = ForEachItem(ItemSearchLocation.Equipment,
								pItem =>
								{
									if (pItem.Entry == item)
									{
										var ires = CanUnequipItem(pItem.Pos, false);

										if (ires == InventoryResult.Ok)
										{
											tempcount += pItem.Count;

											if (tempcount >= count)
												return false;
										}
										else
										{
											res = ires;
										}
									}

									return true;
								});

		if (!result) // we stopped early due to a sucess
			return InventoryResult.Ok;

		return res; // return latest error if any
	}

	void QuickEquipItem(ushort pos, Item pItem)
	{
		if (pItem != null)
		{
			AddEnchantmentDurations(pItem);
			AddItemDurations(pItem);

			var slot = (byte)(pos & 255);
			VisualizeItem(slot, pItem);

			pItem.SetItemFlag2(ItemFieldFlags2.Equipped);

			if (IsInWorld)
			{
				pItem.AddToWorld();
				pItem.SendUpdateToPlayer(this);
			}

			if (slot == EquipmentSlot.MainHand || slot == EquipmentSlot.OffHand)
				CheckTitanGripPenalty();

			UpdateCriteria(CriteriaType.EquipItem, pItem.Entry);
			UpdateCriteria(CriteriaType.EquipItemInSlot, slot, pItem.Entry);
		}
	}

	bool _StoreOrEquipNewItem(uint vendorslot, uint item, byte count, byte bag, byte slot, long price, ItemTemplate pProto, Creature pVendor, VendorItem crItem, bool bStore)
	{
		var stacks = count / pProto.BuyCount;
		List<ItemPosCount> vDest = new();
		ushort uiDest = 0;
		var msg = bStore ? CanStoreNewItem(bag, slot, vDest, item, count) : CanEquipNewItem(slot, out uiDest, item, false);

		if (msg != InventoryResult.Ok)
		{
			SendEquipError(msg, null, null, item);

			return false;
		}

		ModifyMoney(-price);

		if (crItem.ExtendedCost != 0) // case for new honor system
		{
			var iece = CliDB.ItemExtendedCostStorage.LookupByKey(crItem.ExtendedCost);

			for (var i = 0; i < ItemConst.MaxItemExtCostItems; ++i)
				if (iece.ItemID[i] != 0)
					DestroyItemCount(iece.ItemID[i], iece.ItemCount[i] * stacks, true);

			for (var i = 0; i < ItemConst.MaxItemExtCostCurrencies; ++i)
			{
				if (iece.Flags.HasAnyFlag((byte)((int)ItemExtendedCostFlags.RequireSeasonEarned1 << i)))
					continue;

				if (iece.CurrencyID[i] != 0)
					RemoveCurrency(iece.CurrencyID[i], (int)(iece.CurrencyCount[i] * stacks), CurrencyDestroyReason.Vendor);
			}
		}

		var it = bStore ? StoreNewItem(vDest, item, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(item), null, ItemContext.Vendor, crItem.BonusListIDs, false) : EquipNewItem(uiDest, item, ItemContext.Vendor, true);

		if (it != null)
		{
			var new_count = pVendor.UpdateVendorItemCurrentCount(crItem, count);

			BuySucceeded packet = new();

			{
				VendorGUID = pVendor.GUID,
				Muid = vendorslot + 1,
				NewQuantity = crItem.Maxcount > 0 ? new_count : 0xFFFFFFFF,
				QuantityBought = count
			}

			dPacket(packet);

			SendNewItem(it, count, true, false, false);

			if (!bStore)
				AutoUnequipOffhandIfNeed();

			if (pProto.HasFlag(ItemFlags.ItemPurchaseRecord) && crItem.ExtendedCost != 0 && pProto.MaxStackSize == 1)
			{
				it.SetItemFlag(ItemFieldFlags.Refundable);
				it.SetRefundRecipient(GUID);
				it.SetPaidMoney((uint)price);
				it.SetPaidExtendedCost(crItem.ExtendedCost);
				it.SaveRefundDataToDB();
				AddRefundReference(it.GUID);
			}

			Session.CollectionMgr.OnItemAdded(it);
		}

		return true;
	}

	//Item Durations
	void RemoveItemDurations(Item item)
	{
		_itemDuration.Remove(item);
	}

	void AddItemDurations(Item item)
	{
		if (item.ItemData.Expiration != 0)
		{
			_itemDuration.Add(item);
			item.SendTimeUpdate(this);
		}
	}

	void UpdateItemDuration(uint time, bool realtimeonly = false)
	{
		if (_itemDuration.Empty())
			return;

		Log.Logger.Debug("Player:UpdateItemDuration({0}, {1})", time, realtimeonly);

		foreach (var item in _itemDuration)
			if (!realtimeonly || item.Template.HasFlag(ItemFlags.RealDuration))
				item.UpdateDuration(this, time);
	}

	void SendEnchantmentDurations()
	{
		foreach (var enchantDuration in _enchantDurations)
			Session.SendItemEnchantTimeUpdate(GUID, enchantDuration.Item.GUID, (uint)enchantDuration.Slot, enchantDuration.Leftduration / 1000);
	}

	void SendItemDurations()
	{
		foreach (var item in _itemDuration)
			item.SendTimeUpdate(this);
	}

	uint GetItemCountWithLimitCategory(uint limitCategory, Item skipItem)
	{
		uint count = 0;

		ForEachItem(ItemSearchLocation.Everywhere,
					item =>
					{
						if (item != skipItem)
						{
							var pProto = item.Template;

							if (pProto != null)
								if (pProto.ItemLimitCategory == limitCategory)
									count += item.Count;
						}

						return true;
					});

		return count;
	}

	void DestroyZoneLimitedItem(bool update, uint new_zone)
	{
		Log.Logger.Debug("STORAGE: DestroyZoneLimitedItem in map {0} and area {1}", Location.MapId, new_zone);

		// in inventory
		var inventoryEnd = InventorySlots.ItemStart + GetInventorySlotCount();

		for (var i = InventorySlots.ItemStart; i < inventoryEnd; i++)
		{
			var pItem = GetItemByPos(InventorySlots.Bag0, i);

			if (pItem)
				if (pItem.IsLimitedToAnotherMapOrZone(Location.MapId, new_zone))
					DestroyItem(InventorySlots.Bag0, i, update);
		}

		// in inventory bags
		for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
		{
			var pBag = GetBagByPos(i);

			if (pBag)
				for (byte j = 0; j < pBag.GetBagSize(); j++)
				{
					var pItem = pBag.GetItemByPos(j);

					if (pItem)
						if (pItem.IsLimitedToAnotherMapOrZone(Location.MapId, new_zone))
							DestroyItem(i, j, update);
				}
		}

		// in equipment and bag list
		for (var i = EquipmentSlot.Start; i < InventorySlots.BagEnd; i++)
		{
			var pItem = GetItemByPos(InventorySlots.Bag0, i);

			if (pItem)
				if (pItem.IsLimitedToAnotherMapOrZone(Location.MapId, new_zone))
					DestroyItem(InventorySlots.Bag0, i, update);
		}
	}

	void ApplyItemEquipSpell(Item item, bool apply, bool formChange = false)
	{
		if (item == null || item.Template.HasFlag(ItemFlags.Legacy))
			return;

		foreach (var effectData in item.Effects)
		{
			// wrong triggering type
			if (apply && effectData.TriggerType != ItemSpelltriggerType.OnEquip)
				continue;

			// check if it is valid spell
			var spellproto = Global.SpellMgr.GetSpellInfo((uint)effectData.SpellID, Difficulty.None);

			if (spellproto == null)
				continue;

			if (effectData.ChrSpecializationID != 0 && effectData.ChrSpecializationID != GetPrimarySpecialization())
				continue;

			ApplyEquipSpell(spellproto, item, apply, formChange);
		}
	}

	void ApplyEquipCooldown(Item pItem)
	{
		if (pItem.Template.HasFlag(ItemFlags.NoEquipCooldown))
			return;

		var now = GameTime.Now();

		foreach (var effectData in pItem.Effects)
		{
			var effectSpellInfo = Global.SpellMgr.GetSpellInfo((uint)effectData.SpellID, Difficulty.None);

			if (effectSpellInfo == null)
				continue;

			// apply proc cooldown to equip auras if we have any
			if (effectData.TriggerType == ItemSpelltriggerType.OnEquip)
			{
				var procEntry = Global.SpellMgr.GetSpellProcEntry(effectSpellInfo);

				if (procEntry == null)
					continue;

				var itemAura = GetAura((uint)effectData.SpellID, GUID, pItem.GUID);

				if (itemAura != null)
					itemAura.AddProcCooldown(procEntry, now);

				continue;
			}

			// no spell
			if (effectData.SpellID == 0)
				continue;

			// wrong triggering type
			if (effectData.TriggerType != ItemSpelltriggerType.OnUse)
				continue;

			// Don't replace longer cooldowns by equip cooldown if we have any.
			if (SpellHistory.GetRemainingCooldown(effectSpellInfo) > TimeSpan.FromSeconds(30))
				continue;

			SpellHistory.AddCooldown((uint)effectData.SpellID, pItem.Entry, TimeSpan.FromSeconds(30));

			ItemCooldown data = new();

			{
				ItemGuid = pItem.GUID,
				SpellID = (uint)effectData.SpellID,
				Cooldown = 30 * Time.InMilliseconds //Always 30secs?
			}

			dPacket(data);
		}
	}

	void _RemoveAllItemMods()
	{
		Log.Logger.Debug("_RemoveAllItemMods start.");

		for (byte i = 0; i < InventorySlots.BagEnd; ++i)
			if (_items[i] != null)
			{
				var proto = _items[i].Template;

				if (proto == null)
					continue;

				// item set bonuses not dependent from item broken state
				if (proto.ItemSet != 0)
					Item.RemoveItemsSetItem(this, _items[i]);

				if (_items[i].IsBroken || !CanUseAttackType(GetAttackBySlot(i, _items[i].Template.InventoryType)))
					continue;

				ApplyItemEquipSpell(_items[i], false);
				ApplyEnchantment(_items[i], false);
				ApplyArtifactPowers(_items[i], false);
			}

		for (byte i = 0; i < InventorySlots.BagEnd; ++i)
			if (_items[i] != null)
			{
				if (_items[i].IsBroken || !CanUseAttackType(GetAttackBySlot(i, _items[i].Template.InventoryType)))
					continue;

				ApplyItemDependentAuras(_items[i], false);
				_ApplyItemBonuses(_items[i], i, false);
			}

		Log.Logger.Debug("_RemoveAllItemMods complete.");
	}

	void _ApplyAllItemMods()
	{
		Log.Logger.Debug("_ApplyAllItemMods start.");

		for (byte i = 0; i < InventorySlots.BagEnd; ++i)
			if (_items[i] != null)
			{
				if (_items[i].IsBroken || !CanUseAttackType(GetAttackBySlot(i, _items[i].Template.InventoryType)))
					continue;

				ApplyItemDependentAuras(_items[i], true);
				_ApplyItemBonuses(_items[i], i, true);

				var attackType = GetAttackBySlot(i, _items[i].Template.InventoryType);

				if (attackType != WeaponAttackType.Max)
					UpdateWeaponDependentAuras(attackType);
			}

		for (byte i = 0; i < InventorySlots.BagEnd; ++i)
			if (_items[i] != null)
			{
				var proto = _items[i].Template;

				if (proto == null)
					continue;

				// item set bonuses not dependent from item broken state
				if (proto.ItemSet != 0)
					Item.AddItemsSetItem(this, _items[i]);

				if (_items[i].IsBroken || !CanUseAttackType(GetAttackBySlot(i, _items[i].Template.InventoryType)))
					continue;

				ApplyItemEquipSpell(_items[i], true);
				ApplyArtifactPowers(_items[i], true);
				ApplyEnchantment(_items[i], true);
			}

		Log.Logger.Debug("_ApplyAllItemMods complete.");
	}

	void ApplyAllAzeriteItemMods(bool apply)
	{
		for (byte i = 0; i < InventorySlots.BagEnd; ++i)
			if (_items[i])
			{
				if (!_items[i].IsAzeriteItem || _items[i].IsBroken || !CanUseAttackType(GetAttackBySlot(i, _items[i].Template.InventoryType)))
					continue;

				ApplyAzeritePowers(_items[i], apply);
			}
	}

	void ApplyAllAzeriteEmpoweredItemMods(bool apply)
	{
		for (byte i = 0; i < InventorySlots.BagEnd; ++i)
			if (_items[i])
			{
				if (!_items[i].IsAzeriteEmpoweredItem || _items[i].IsBroken || !CanUseAttackType(GetAttackBySlot(i, _items[i].Template.InventoryType)))
					continue;

				ApplyAzeritePowers(_items[i], apply);
			}
	}

	InventoryResult CanStoreItem_InInventorySlots(byte slot_begin, byte slot_end, List<ItemPosCount> dest, ItemTemplate pProto, ref uint count, bool merge, Item pSrcItem, byte skip_bag, byte skip_slot)
	{
		//this is never called for non-bag slots so we can do this
		if (pSrcItem != null && pSrcItem.IsNotEmptyBag)
			return InventoryResult.DestroyNonemptyBag;

		for (var j = slot_begin; j < slot_end; j++)
		{
			// skip specific slot already processed in first called CanStoreItem_InSpecificSlot
			if (InventorySlots.Bag0 == skip_bag && j == skip_slot)
				continue;

			var pItem2 = GetItemByPos(InventorySlots.Bag0, j);

			// ignore move item (this slot will be empty at move)
			if (pItem2 == pSrcItem)
				pItem2 = null;

			// if merge skip empty, if !merge skip non-empty
			if ((pItem2 != null) != merge)
				continue;

			var need_space = pProto.MaxStackSize;

			if (pItem2 != null)
			{
				// can be merged at least partly
				var res = pItem2.CanBeMergedPartlyWith(pProto);

				if (res != InventoryResult.Ok)
					continue;

				// descrease at current stacksize
				need_space -= pItem2.Count;
			}

			if (need_space > count)
				need_space = count;

			ItemPosCount newPosition = new((ushort)(InventorySlots.Bag0 << 8 | j), need_space);

			if (!newPosition.IsContainedIn(dest))
			{
				dest.Add(newPosition);
				count -= need_space;

				if (count == 0)
					return InventoryResult.Ok;
			}
		}

		return InventoryResult.Ok;
	}

	InventoryResult CanStoreItem_InSpecificSlot(byte bag, byte slot, List<ItemPosCount> dest, ItemTemplate pProto, ref uint count, bool swap, Item pSrcItem)
	{
		var pItem2 = GetItemByPos(bag, slot);

		// ignore move item (this slot will be empty at move)
		if (pItem2 == pSrcItem)
			pItem2 = null;

		uint need_space;

		if (pSrcItem)
		{
			if (pSrcItem.IsNotEmptyBag && !IsBagPos((ushort)((ushort)bag << 8 | slot)))
				return InventoryResult.DestroyNonemptyBag;

			if (pSrcItem.HasItemFlag(ItemFieldFlags.Child) && !IsEquipmentPos(bag, slot) && !IsChildEquipmentPos(bag, slot))
				return InventoryResult.WrongBagType3;

			if (!pSrcItem.HasItemFlag(ItemFieldFlags.Child) && IsChildEquipmentPos(bag, slot))
				return InventoryResult.WrongBagType3;
		}

		// empty specific slot - check item fit to slot
		if (pItem2 == null || swap)
		{
			if (bag == InventorySlots.Bag0)
			{
				// prevent cheating
				if ((slot >= InventorySlots.BuyBackStart && slot < InventorySlots.BuyBackEnd) || slot >= (byte)PlayerSlots.End)
					return InventoryResult.WrongBagType;

				// can't store anything else than crafting reagents in Reagent Bank
				if (IsReagentBankPos(bag, slot) && (!IsReagentBankUnlocked || !pProto.IsCraftingReagent))
					return InventoryResult.WrongBagType;
			}
			else
			{
				var pBag = GetBagByPos(bag);

				if (pBag == null)
					return InventoryResult.WrongBagType;

				var pBagProto = pBag.Template;

				if (pBagProto == null)
					return InventoryResult.WrongBagType;

				if (slot >= pBagProto.ContainerSlots)
					return InventoryResult.WrongBagType;

				if (!Item.ItemCanGoIntoBag(pProto, pBagProto))
					return InventoryResult.WrongBagType;
			}

			// non empty stack with space
			need_space = pProto.MaxStackSize;
		}
		// non empty slot, check item type
		else
		{
			// can be merged at least partly
			var res = pItem2.CanBeMergedPartlyWith(pProto);

			if (res != InventoryResult.Ok)
				return res;

			// free stack space or infinity
			need_space = pProto.MaxStackSize - pItem2.Count;
		}

		if (need_space > count)
			need_space = count;

		ItemPosCount newPosition = new((ushort)(bag << 8 | slot), need_space);

		if (!newPosition.IsContainedIn(dest))
		{
			dest.Add(newPosition);
			count -= need_space;
		}

		return InventoryResult.Ok;
	}

	InventoryResult CanStoreItem_InBag(byte bag, List<ItemPosCount> dest, ItemTemplate pProto, ref uint count, bool merge, bool non_specialized, Item pSrcItem, byte skip_bag, byte skip_slot)
	{
		// skip specific bag already processed in first called CanStoreItem_InBag
		if (bag == skip_bag)
			return InventoryResult.WrongBagType;

		// skip not existed bag or self targeted bag
		var pBag = GetBagByPos(bag);

		if (pBag == null || pBag == pSrcItem)
			return InventoryResult.WrongBagType;

		if (pSrcItem)
		{
			if (pSrcItem.IsNotEmptyBag)
				return InventoryResult.DestroyNonemptyBag;

			if (pSrcItem.HasItemFlag(ItemFieldFlags.Child))
				return InventoryResult.WrongBagType3;
		}

		var pBagProto = pBag.Template;

		if (pBagProto == null)
			return InventoryResult.WrongBagType;

		// specialized bag mode or non-specilized
		if (non_specialized != (pBagProto.Class == ItemClass.Container && pBagProto.SubClass == (uint)ItemSubClassContainer.Container))
			return InventoryResult.WrongBagType;

		if (!Item.ItemCanGoIntoBag(pProto, pBagProto))
			return InventoryResult.WrongBagType;

		for (byte j = 0; j < pBag.GetBagSize(); j++)
		{
			// skip specific slot already processed in first called CanStoreItem_InSpecificSlot
			if (j == skip_slot)
				continue;

			var pItem2 = GetItemByPos(bag, j);

			// ignore move item (this slot will be empty at move)
			if (pItem2 == pSrcItem)
				pItem2 = null;

			// if merge skip empty, if !merge skip non-empty
			if ((pItem2 != null) != merge)
				continue;

			var need_space = pProto.MaxStackSize;

			if (pItem2 != null)
			{
				// can be merged at least partly
				var res = pItem2.CanBeMergedPartlyWith(pProto);

				if (res != InventoryResult.Ok)
					continue;

				// descrease at current stacksize
				need_space -= pItem2.Count;
			}

			if (need_space > count)
				need_space = count;

			ItemPosCount newPosition = new((ushort)(bag << 8 | j), need_space);

			if (!newPosition.IsContainedIn(dest))
			{
				dest.Add(newPosition);
				count -= need_space;

				if (count == 0)
					return InventoryResult.Ok;
			}
		}

		return InventoryResult.Ok;
	}

	byte FindEquipSlot(Item item, uint slot, bool swap)
	{
		var slots = new byte[4];
		slots[0] = ItemConst.NullSlot;
		slots[1] = ItemConst.NullSlot;
		slots[2] = ItemConst.NullSlot;
		slots[3] = ItemConst.NullSlot;

		switch (item.Template.InventoryType)
		{
			case InventoryType.Head:
				slots[0] = EquipmentSlot.Head;

				break;
			case InventoryType.Neck:
				slots[0] = EquipmentSlot.Neck;

				break;
			case InventoryType.Shoulders:
				slots[0] = EquipmentSlot.Shoulders;

				break;
			case InventoryType.Body:
				slots[0] = EquipmentSlot.Shirt;

				break;
			case InventoryType.Chest:
				slots[0] = EquipmentSlot.Chest;

				break;
			case InventoryType.Robe:
				slots[0] = EquipmentSlot.Chest;

				break;
			case InventoryType.Waist:
				slots[0] = EquipmentSlot.Waist;

				break;
			case InventoryType.Legs:
				slots[0] = EquipmentSlot.Legs;

				break;
			case InventoryType.Feet:
				slots[0] = EquipmentSlot.Feet;

				break;
			case InventoryType.Wrists:
				slots[0] = EquipmentSlot.Wrist;

				break;
			case InventoryType.Hands:
				slots[0] = EquipmentSlot.Hands;

				break;
			case InventoryType.Finger:
				slots[0] = EquipmentSlot.Finger1;
				slots[1] = EquipmentSlot.Finger2;

				break;
			case InventoryType.Trinket:
				slots[0] = EquipmentSlot.Trinket1;
				slots[1] = EquipmentSlot.Trinket2;

				break;
			case InventoryType.Cloak:
				slots[0] = EquipmentSlot.Cloak;

				break;
			case InventoryType.Weapon:
			{
				slots[0] = EquipmentSlot.MainHand;

				// suggest offhand slot only if know dual wielding
				// (this will be replace mainhand weapon at auto equip instead unwonted "you don't known dual wielding" ...
				if (CanDualWield)
					slots[1] = EquipmentSlot.OffHand;

				break;
			}
			case InventoryType.Shield:
				slots[0] = EquipmentSlot.OffHand;

				break;
			case InventoryType.Ranged:
				slots[0] = EquipmentSlot.MainHand;

				break;
			case InventoryType.Weapon2Hand:
				slots[0] = EquipmentSlot.MainHand;

				if (CanDualWield && CanTitanGrip())
					slots[1] = EquipmentSlot.OffHand;

				break;
			case InventoryType.Tabard:
				slots[0] = EquipmentSlot.Tabard;

				break;
			case InventoryType.WeaponMainhand:
				slots[0] = EquipmentSlot.MainHand;

				break;
			case InventoryType.WeaponOffhand:
				slots[0] = EquipmentSlot.OffHand;

				break;
			case InventoryType.Holdable:
				slots[0] = EquipmentSlot.OffHand;

				break;
			case InventoryType.RangedRight:
				slots[0] = EquipmentSlot.MainHand;

				break;
			case InventoryType.Bag:
				slots[0] = InventorySlots.BagStart + 0;
				slots[1] = InventorySlots.BagStart + 1;
				slots[2] = InventorySlots.BagStart + 2;
				slots[3] = InventorySlots.BagStart + 3;

				break;
			case InventoryType.ProfessionTool:
			case InventoryType.ProfessionGear:
			{
				var isProfessionTool = item.Template.InventoryType == InventoryType.ProfessionTool;

				// Validate item class
				if (!(item.Template.Class == ItemClass.Profession))
					return ItemConst.NullSlot;

				// Check if player has profession skill
				var itemSkill = (uint)item.Template.GetSkill();

				if (!HasSkill(itemSkill))
					return ItemConst.NullSlot;

				switch ((ItemSubclassProfession)item.Template.SubClass)
				{
					case ItemSubclassProfession.Cooking:
						slots[0] = isProfessionTool ? ProfessionSlots.CookingTool : ProfessionSlots.CookingGear1;

						break;
					case ItemSubclassProfession.Fishing:
					{
						// Fishing doesn't make use of gear slots (clientside)
						if (!isProfessionTool)
							return ItemConst.NullSlot;

						slots[0] = ProfessionSlots.FishingTool;

						break;
					}
					case ItemSubclassProfession.Blacksmithing:
					case ItemSubclassProfession.Leatherworking:
					case ItemSubclassProfession.Alchemy:
					case ItemSubclassProfession.Herbalism:
					case ItemSubclassProfession.Mining:
					case ItemSubclassProfession.Tailoring:
					case ItemSubclassProfession.Engineering:
					case ItemSubclassProfession.Enchanting:
					case ItemSubclassProfession.Skinning:
					case ItemSubclassProfession.Jewelcrafting:
					case ItemSubclassProfession.Inscription:
					{
						var professionSlot = GetProfessionSlotFor(itemSkill);

						if (professionSlot == -1)
							return ItemConst.NullSlot;

						if (isProfessionTool)
						{
							slots[0] = (byte)(ProfessionSlots.Profession1Tool + professionSlot * ProfessionSlots.MaxCount);
						}
						else
						{
							slots[0] = (byte)(ProfessionSlots.Profession1Gear1 + professionSlot * ProfessionSlots.MaxCount);
							slots[0] = (byte)(ProfessionSlots.Profession1Gear2 + professionSlot * ProfessionSlots.MaxCount);
						}

						break;
					}
					default:
						return ItemConst.NullSlot;
				}

				break;
			}
			default:
				return ItemConst.NullSlot;
		}


		if (slot != ItemConst.NullSlot)
		{
			if (swap || GetItemByPos(InventorySlots.Bag0, (byte)slot) == null)
				for (byte i = 0; i < 4; ++i)
					if (slots[i] == slot)
						return (byte)slot;
		}
		else
		{
			// search free slot at first
			for (byte i = 0; i < 4; ++i)
				if (slots[i] != ItemConst.NullSlot && GetItemByPos(InventorySlots.Bag0, slots[i]) == null)
					// in case 2hand equipped weapon (without titan grip) offhand slot empty but not free
					if (slots[i] != EquipmentSlot.OffHand || !IsTwoHandUsed())
						return slots[i];

			// if not found free and can swap return slot with lower item level equipped
			if (swap)
			{
				var minItemLevel = uint.MaxValue;
				byte minItemLevelIndex = 0;

				for (byte i = 0; i < 4; ++i)
					if (slots[i] != ItemConst.NullSlot)
					{
						var equipped = GetItemByPos(InventorySlots.Bag0, slots[i]);

						if (equipped != null)
						{
							var itemLevel = equipped.GetItemLevel(this);

							if (itemLevel < minItemLevel)
							{
								minItemLevel = itemLevel;
								minItemLevelIndex = i;
							}
						}
					}

				return slots[minItemLevelIndex];
			}
		}

		// no free position
		return ItemConst.NullSlot;
	}

	InventoryResult CanEquipNewItem(byte slot, out ushort dest, uint item, bool swap)
	{
		dest = 0;
		var pItem = Item.CreateItem(item, 1, ItemContext.None, this);

		if (pItem != null)
		{
			var result = CanEquipItem(slot, out dest, pItem, swap);

			return result;
		}

		return InventoryResult.ItemNotFound;
	}

	//Artifact
	void ApplyArtifactPowers(Item item, bool apply)
	{
		if (item.IsArtifactDisabled())
			return;

		foreach (var artifactPower in item.ItemData.ArtifactPowers)
		{
			var rank = artifactPower.CurrentRankWithBonus;

			if (rank == 0)
				continue;

			if (CliDB.ArtifactPowerStorage[artifactPower.ArtifactPowerId].Flags.HasAnyFlag(ArtifactPowerFlag.ScalesWithNumPowers))
				rank = 1;

			var artifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(artifactPower.ArtifactPowerId, (byte)(rank - 1));

			if (artifactPowerRank == null)
				continue;

			ApplyArtifactPowerRank(item, artifactPowerRank, apply);
		}

		var artifactAppearance = CliDB.ArtifactAppearanceStorage.LookupByKey(item.GetModifier(ItemModifier.ArtifactAppearanceId));

		if (artifactAppearance != null)
			if (artifactAppearance.OverrideShapeshiftDisplayID != 0 && ShapeshiftForm == (ShapeShiftForm)artifactAppearance.OverrideShapeshiftFormID)
				RestoreDisplayId();
	}

	void ApplyAzeritePowers(Item item, bool apply)
	{
		var azeriteItem = item.AsAzeriteItem;

		if (azeriteItem != null)
		{
			// milestone powers
			foreach (var azeriteItemMilestonePowerId in azeriteItem.AzeriteItemData.UnlockedEssenceMilestones)
				ApplyAzeriteItemMilestonePower(azeriteItem, CliDB.AzeriteItemMilestonePowerStorage.LookupByKey(azeriteItemMilestonePowerId), apply);

			// essences
			var selectedEssences = azeriteItem.GetSelectedAzeriteEssences();

			if (selectedEssences != null)
				for (byte slot = 0; slot < SharedConst.MaxAzeriteEssenceSlot; ++slot)
					if (selectedEssences.AzeriteEssenceID[slot] != 0)
						ApplyAzeriteEssence(azeriteItem,
											selectedEssences.AzeriteEssenceID[slot],
											azeriteItem.GetEssenceRank(selectedEssences.AzeriteEssenceID[slot]),
											(AzeriteItemMilestoneType)Global.DB2Mgr.GetAzeriteItemMilestonePower(slot).Type == AzeriteItemMilestoneType.MajorEssence,
											apply);
		}
		else
		{
			var azeriteEmpoweredItem = item.AsAzeriteEmpoweredItem;

			if (azeriteEmpoweredItem)
				if (!apply || GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Equipment))
					for (var i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
					{
						var azeritePower = CliDB.AzeritePowerStorage.LookupByKey(azeriteEmpoweredItem.GetSelectedAzeritePower(i));

						if (azeritePower != null)
							ApplyAzeritePower(azeriteEmpoweredItem, azeritePower, apply);
					}
		}
	}

	void ApplyAzeriteEssencePower(AzeriteItem item, AzeriteEssencePowerRecord azeriteEssencePower, bool major, bool apply)
	{
		var powerSpell = Global.SpellMgr.GetSpellInfo(azeriteEssencePower.MinorPowerDescription, Difficulty.None);

		if (powerSpell != null)
		{
			if (apply)
				CastSpell(this, powerSpell.Id, item);
			else
				RemoveAurasDueToItemSpell(powerSpell.Id, item.GUID);
		}

		if (major)
		{
			powerSpell = Global.SpellMgr.GetSpellInfo(azeriteEssencePower.MajorPowerDescription, Difficulty.None);

			if (powerSpell != null)
			{
				if (powerSpell.IsPassive)
				{
					if (apply)
						CastSpell(this, powerSpell.Id, item);
					else
						RemoveAurasDueToItemSpell(powerSpell.Id, item.GUID);
				}
				else
				{
					if (apply)
						LearnSpell(powerSpell.Id, true, 0, true);
					else
						RemoveSpell(powerSpell.Id, false, false, true);
				}
			}
		}
	}

	bool HasItemWithLimitCategoryEquipped(uint limitCategory, uint count, byte except_slot)
	{
		uint tempcount = 0;

		return !ForEachItem(ItemSearchLocation.Equipment,
							pItem =>
							{
								if (pItem.Slot == except_slot)
									return true;

								if (pItem.Template.ItemLimitCategory != limitCategory)
									return true;

								tempcount += pItem.Count;

								if (tempcount >= count)
									return false;

								return true;
							});
	}

	bool HasGemWithLimitCategoryEquipped(uint limitCategory, uint count, byte except_slot)
	{
		uint tempcount = 0;

		return !ForEachItem(ItemSearchLocation.Equipment,
							pItem =>
							{
								if (pItem.Slot == except_slot)
									return true;

								var pProto = pItem.Template;

								if (pProto == null)
									return true;

								tempcount += pItem.GetGemCountWithLimitCategory(limitCategory);

								if (tempcount >= count)
									return false;

								return true;
							});
	}

	void VisualizeItem(uint slot, Item pItem)
	{
		if (pItem == null)
			return;

		// check also  BIND_WHEN_PICKED_UP and BIND_QUEST_ITEM for .additem or .additemset case by GM (not binded at adding to inventory)
		if (pItem.Bonding == ItemBondingType.OnEquip || pItem.Bonding == ItemBondingType.OnAcquire || pItem.Bonding == ItemBondingType.Quest)
		{
			pItem.SetBinding(true);

			if (IsInWorld)
				Session.CollectionMgr.AddItemAppearance(pItem);
		}

		Log.Logger.Debug("STORAGE: EquipItem slot = {0}, item = {1}", slot, pItem.Entry);

		_items[slot] = pItem;
		SetInvSlot(slot, pItem.GUID);
		pItem.SetContainedIn(GUID);
		pItem.SetOwnerGUID(GUID);
		pItem.SetSlot((byte)slot);
		pItem.SetContainer(null);

		if (slot < EquipmentSlot.End)
			SetVisibleItemSlot(slot, pItem);

		pItem.SetState(ItemUpdateState.Changed, this);
	}

	void AutoStoreLoot(byte bag, byte slot, uint loot_id, LootStore store, ItemContext context = 0, bool broadcast = false, bool createdByPlayer = false)
	{
		Loot.Loot loot = new(null, ObjectGuid.Empty, LootType.None, null);
		loot.FillLoot(loot_id, store, this, true, false, LootModes.Default, context);

		loot.AutoStore(this, bag, slot, broadcast, createdByPlayer);
		ProcSkillsAndAuras(this, null, new ProcFlagsInit(ProcFlags.Looted), new ProcFlagsInit(ProcFlags.None), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
	}

	void SendEquipmentSetList()
	{
		LoadEquipmentSet data = new();

		foreach (var pair in _equipmentSets)
		{
			if (pair.Value.State == EquipmentSetUpdateState.Deleted)
				continue;

			data.SetData.Add(pair.Value.Data);
		}

		SendPacket(data);
	}

	//Misc
	void UpdateItemLevelAreaBasedScaling()
	{
		// @todo Activate pvp item levels during world pvp
		var map = Map;
		var pvpActivity = map.IsBattlegroundOrArena || ((int)map.Entry.Flags[1]).HasAnyFlag(0x40) || HasPvpRulesEnabled();

		if (_usePvpItemLevels != pvpActivity)
		{
			var healthPct = HealthPct;
			_RemoveAllItemMods();
			ActivatePvpItemLevels(pvpActivity);
			_ApplyAllItemMods();
			SetHealth(MathFunctions.CalculatePct(MaxHealth, healthPct));
		}
		// @todo other types of power scaling such as timewalking
	}

	bool ForEachEquipmentSlot(InventoryType inventoryType, bool canDualWield, bool canTitanGrip, EquipmentSlotDelegate callback)
	{
		switch (inventoryType)
		{
			case InventoryType.Head:
				callback(EquipmentSlot.Head);

				return true;
			case InventoryType.Neck:
				callback(EquipmentSlot.Neck);

				return true;
			case InventoryType.Shoulders:
				callback(EquipmentSlot.Shoulders);

				return true;
			case InventoryType.Body:
				callback(EquipmentSlot.Shirt);

				return true;
			case InventoryType.Robe:
			case InventoryType.Chest:
				callback(EquipmentSlot.Chest);

				return true;
			case InventoryType.Waist:
				callback(EquipmentSlot.Waist);

				return true;
			case InventoryType.Legs:
				callback(EquipmentSlot.Legs);

				return true;
			case InventoryType.Feet:
				callback(EquipmentSlot.Feet);

				return true;
			case InventoryType.Wrists:
				callback(EquipmentSlot.Wrist);

				return true;
			case InventoryType.Hands:
				callback(EquipmentSlot.Hands);

				return true;
			case InventoryType.Cloak:
				callback(EquipmentSlot.Cloak);

				return true;
			case InventoryType.Finger:
				callback(EquipmentSlot.Finger1);
				callback(EquipmentSlot.Finger2, true);

				return true;
			case InventoryType.Trinket:
				callback(EquipmentSlot.Trinket1);
				callback(EquipmentSlot.Trinket2, true);

				return true;
			case InventoryType.Weapon:
				callback(EquipmentSlot.MainHand);

				if (canDualWield)
					callback(EquipmentSlot.OffHand, true);

				return true;
			case InventoryType.Weapon2Hand:
				callback(EquipmentSlot.MainHand);

				if (canDualWield && canTitanGrip)
					callback(EquipmentSlot.OffHand, true);

				return true;
			case InventoryType.Ranged:
			case InventoryType.RangedRight:
			case InventoryType.WeaponMainhand:
				callback(EquipmentSlot.MainHand);

				return true;
			case InventoryType.Shield:
			case InventoryType.Holdable:
			case InventoryType.WeaponOffhand:
				callback(EquipmentSlot.OffHand);

				return true;
			default:
				return false;
		}
	}

	delegate void EquipmentSlotDelegate(byte equipmentSlot, bool checkDuplicateGuid = false);
}