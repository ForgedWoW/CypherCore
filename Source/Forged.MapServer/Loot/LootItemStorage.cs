// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Players;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Loot;

public class LootItemStorage : Singleton<LootItemStorage>
{
	readonly ConcurrentDictionary<ulong, StoredLootContainer> _lootItemStorage = new();
	LootItemStorage() { }

	public void LoadStorageFromDB()
	{
		var oldMSTime = Time.MSTime;
		_lootItemStorage.Clear();
		uint count = 0;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEMCONTAINER_ITEMS);
		var result = DB.Characters.Query(stmt);

		if (!result.IsEmpty())
		{
			do
			{
				var key = result.Read<ulong>(0);

				if (!_lootItemStorage.ContainsKey(key))
					_lootItemStorage[key] = new StoredLootContainer(key);

				var storedContainer = _lootItemStorage[key];

				LootItem lootItem = new()
				{
					itemid = result.Read<uint>(1),
					count = result.Read<byte>(2),
					LootListId = result.Read<uint>(3),
					follow_loot_rules = result.Read<bool>(4),
					freeforall = result.Read<bool>(5),
					is_blocked = result.Read<bool>(6),
					is_counted = result.Read<bool>(7),
					is_underthreshold = result.Read<bool>(8),
					needs_quest = result.Read<bool>(9),
					randomBonusListId = result.Read<uint>(10),
					context = (ItemContext)result.Read<byte>(11)
				};

				StringArray bonusLists = new(result.Read<string>(12), ' ');

				if (bonusLists != null && !bonusLists.IsEmpty())
					foreach (string str in bonusLists)
						lootItem.BonusListIDs.Add(uint.Parse(str));

				storedContainer.AddLootItem(lootItem, null);

				++count;
			} while (result.NextRow());

			Log.Logger.Information($"Loaded {count} stored item loots in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
		}
		else
		{
			Log.Logger.Information("Loaded 0 stored item loots");
		}

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEMCONTAINER_MONEY);
		result = DB.Characters.Query(stmt);

		if (!result.IsEmpty())
		{
			count = 0;

			do
			{
				var key = result.Read<ulong>(0);

				if (!_lootItemStorage.ContainsKey(key))
					_lootItemStorage.TryAdd(key, new StoredLootContainer(key));

				var storedContainer = _lootItemStorage[key];
				storedContainer.AddMoney(result.Read<uint>(1), null);

				++count;
			} while (result.NextRow());

			Log.Logger.Information($"Loaded {count} stored item money in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
		}
		else
		{
			Log.Logger.Information("Loaded 0 stored item money");
		}
	}

	public bool LoadStoredLoot(Item item, Player player)
	{
		if (!_lootItemStorage.ContainsKey(item.GUID.Counter))
			return false;

		var container = _lootItemStorage[item.GUID.Counter];

		Loot loot = new(player.Map, item.GUID, LootType.Item, null)
		{
			gold = container.GetMoney()
		};

		var lt = LootStorage.Items.GetLootFor(item.Entry);

		if (lt != null)
			foreach (var (id, storedItem) in container.GetLootItems().KeyValueList)
			{
				LootItem li = new()
				{
					itemid = id,
					count = (byte)storedItem.Count,
					LootListId = storedItem.ItemIndex,
					follow_loot_rules = storedItem.FollowRules,
					freeforall = storedItem.FFA,
					is_blocked = storedItem.Blocked,
					is_counted = storedItem.Counted,
					is_underthreshold = storedItem.UnderThreshold,
					needs_quest = storedItem.NeedsQuest,
					randomBonusListId = storedItem.RandomBonusListId,
					context = storedItem.Context,
					BonusListIDs = storedItem.BonusListIDs
				};

				// Copy the extra loot conditions from the item in the loot template
				lt.CopyConditions(li);

				// If container item is in a bag, add that player as an allowed looter
				if (item.BagSlot != 0)
					li.AddAllowedLooter(player);

				// Finally add the LootItem to the container
				loot.items.Add(li);

				// Increment unlooted count
				++loot.unlootedCount;
			}

		// Mark the item if it has loot so it won't be generated again on open
		item.Loot = loot;
		item.LootGenerated = true;

		return true;
	}

	public void RemoveStoredMoneyForContainer(ulong containerId)
	{
		if (!_lootItemStorage.ContainsKey(containerId))
			return;

		_lootItemStorage[containerId].RemoveMoney();
	}

	public void RemoveStoredLootForContainer(ulong containerId)
	{
		_lootItemStorage.TryRemove(containerId, out _);

		SQLTransaction trans = new();
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEMS);
		stmt.AddValue(0, containerId);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_MONEY);
		stmt.AddValue(0, containerId);
		trans.Append(stmt);

		DB.Characters.CommitTransaction(trans);
	}

	public void RemoveStoredLootItemForContainer(ulong containerId, uint itemId, uint count, uint itemIndex)
	{
		if (!_lootItemStorage.ContainsKey(containerId))
			return;

		_lootItemStorage[containerId].RemoveItem(itemId, count, itemIndex);
	}

	public void AddNewStoredLoot(ulong containerId, Loot loot, Player player)
	{
		// Saves the money and item loot associated with an openable item to the DB
		if (loot.IsLooted()) // no money and no loot
			return;

		if (_lootItemStorage.ContainsKey(containerId))
		{
			Log.Logger.Error($"Trying to store item loot by player: {player.GUID} for container id: {containerId} that is already in storage!");

			return;
		}

		StoredLootContainer container = new(containerId);

		SQLTransaction trans = new();

		if (loot.gold != 0)
			container.AddMoney(loot.gold, trans);

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEMS);
		stmt.AddValue(0, containerId);
		trans.Append(stmt);

		foreach (var li in loot.items)
		{
			// Conditions are not checked when loot is generated, it is checked when loot is sent to a player.
			// For items that are lootable, loot is saved to the DB immediately, that means that loot can be
			// saved to the DB that the player never should have gotten. This check prevents that, so that only
			// items that the player should get in loot are in the DB.
			// IE: Horde items are not saved to the DB for Ally players.
			if (!li.AllowedForPlayer(player, loot))
				continue;

			// Don't save currency tokens
			var itemTemplate = Global.ObjectMgr.GetItemTemplate(li.itemid);

			if (itemTemplate == null || itemTemplate.IsCurrencyToken)
				continue;

			container.AddLootItem(li, trans);
		}

		DB.Characters.CommitTransaction(trans);

		_lootItemStorage.TryAdd(containerId, container);
	}
}

class StoredLootContainer
{
	readonly MultiMap<uint, StoredLootItem> _lootItems = new();
	readonly ulong _containerId;
	uint _money;

	public StoredLootContainer(ulong containerId)
	{
		_containerId = containerId;
	}

	public void AddLootItem(LootItem lootItem, SQLTransaction trans)
	{
		_lootItems.Add(lootItem.itemid, new StoredLootItem(lootItem));

		if (trans == null)
			return;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEMCONTAINER_ITEMS);

		// container_id, item_id, item_count, follow_rules, ffa, blocked, counted, under_threshold, needs_quest, rnd_prop, rnd_suffix
		stmt.AddValue(0, _containerId);
		stmt.AddValue(1, lootItem.itemid);
		stmt.AddValue(2, lootItem.count);
		stmt.AddValue(3, lootItem.LootListId);
		stmt.AddValue(4, lootItem.follow_loot_rules);
		stmt.AddValue(5, lootItem.freeforall);
		stmt.AddValue(6, lootItem.is_blocked);
		stmt.AddValue(7, lootItem.is_counted);
		stmt.AddValue(8, lootItem.is_underthreshold);
		stmt.AddValue(9, lootItem.needs_quest);
		stmt.AddValue(10, lootItem.randomBonusListId);
		stmt.AddValue(11, (uint)lootItem.context);

		StringBuilder bonusListIDs = new();

		foreach (int bonusListID in lootItem.BonusListIDs)
			bonusListIDs.Append(bonusListID + ' ');

		stmt.AddValue(12, bonusListIDs.ToString());
		trans.Append(stmt);
	}

	public void AddMoney(uint money, SQLTransaction trans)
	{
		_money = money;

		if (trans == null)
			return;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_MONEY);
		stmt.AddValue(0, _containerId);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEMCONTAINER_MONEY);
		stmt.AddValue(0, _containerId);
		stmt.AddValue(1, _money);
		trans.Append(stmt);
	}

	public void RemoveMoney()
	{
		_money = 0;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_MONEY);
		stmt.AddValue(0, _containerId);
		DB.Characters.Execute(stmt);
	}

	public void RemoveItem(uint itemId, uint count, uint itemIndex)
	{
		var bounds = _lootItems.LookupByKey(itemId);

		foreach (var itr in bounds)
			if (itr.Count == count)
			{
				_lootItems.Remove(itr.ItemId);

				break;
			}

		// Deletes a single item associated with an openable item from the DB
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEM);
		stmt.AddValue(0, _containerId);
		stmt.AddValue(1, itemId);
		stmt.AddValue(2, count);
		stmt.AddValue(3, itemIndex);
		DB.Characters.Execute(stmt);
	}

	public uint GetMoney()
	{
		return _money;
	}

	public MultiMap<uint, StoredLootItem> GetLootItems()
	{
		return _lootItems;
	}

	ulong GetContainer()
	{
		return _containerId;
	}
}

class StoredLootItem
{
	public uint ItemId;
	public uint Count;
	public uint ItemIndex;
	public bool FollowRules;
	public bool FFA;
	public bool Blocked;
	public bool Counted;
	public bool UnderThreshold;
	public bool NeedsQuest;
	public uint RandomBonusListId;
	public ItemContext Context;
	public List<uint> BonusListIDs = new();

	public StoredLootItem(LootItem lootItem)
	{
		ItemId = lootItem.itemid;
		Count = lootItem.count;
		ItemIndex = lootItem.LootListId;
		FollowRules = lootItem.follow_loot_rules;
		FFA = lootItem.freeforall;
		Blocked = lootItem.is_blocked;
		Counted = lootItem.is_counted;
		UnderThreshold = lootItem.is_underthreshold;
		NeedsQuest = lootItem.needs_quest;
		RandomBonusListId = lootItem.randomBonusListId;
		Context = lootItem.context;
		BonusListIDs = lootItem.BonusListIDs;
	}
}