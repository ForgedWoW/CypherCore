// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Concurrent;
using Forged.MapServer.Conditions;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.LootManagement;

public class LootItemStorage
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly ConditionManager _conditionManager;
    private readonly LootFactory _lootFactory;
    private readonly ConcurrentDictionary<ulong, StoredLootContainer> _lootItemStorage = new();
    private readonly LootStoreBox _lootStorage;
    private readonly GameObjectManager _objectManager;

    public LootItemStorage(CharacterDatabase characterDatabase, GameObjectManager objectManager, ConditionManager conditionManager, LootFactory lootFactory, LootStoreBox lootStorage)
    {
        _characterDatabase = characterDatabase;
        _objectManager = objectManager;
        _conditionManager = conditionManager;
        _lootFactory = lootFactory;
        _lootStorage = lootStorage;
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

        StoredLootContainer container = new(containerId, _characterDatabase);

        SQLTransaction trans = new();

        if (loot.Gold != 0)
            container.AddMoney(loot.Gold, trans);

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEMS);
        stmt.AddValue(0, containerId);
        trans.Append(stmt);

        foreach (var li in loot.Items)
        {
            // Conditions are not checked when loot is generated, it is checked when loot is sent to a player.
            // For items that are lootable, loot is saved to the DB immediately, that means that loot can be
            // saved to the DB that the player never should have gotten. This check prevents that, so that only
            // items that the player should get in loot are in the DB.
            // IE: Horde items are not saved to the DB for Ally players.
            if (!li.AllowedForPlayer(player, loot))
                continue;

            // Don't save currency tokens
            var itemTemplate = _objectManager.GetItemTemplate(li.Itemid);

            if (itemTemplate == null || itemTemplate.IsCurrencyToken)
                continue;

            container.AddLootItem(li, trans);
        }

        _characterDatabase.CommitTransaction(trans);

        _lootItemStorage.TryAdd(containerId, container);
    }

    public void LoadStorageFromDB()
    {
        var oldMSTime = Time.MSTime;
        _lootItemStorage.Clear();
        uint count = 0;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_ITEMCONTAINER_ITEMS);
        var result = _characterDatabase.Query(stmt);

        if (!result.IsEmpty())
        {
            do
            {
                var key = result.Read<ulong>(0);

                if (!_lootItemStorage.ContainsKey(key))
                    _lootItemStorage[key] = new StoredLootContainer(key, _characterDatabase);

                var storedContainer = _lootItemStorage[key];

                LootItem lootItem = new(_objectManager, _conditionManager)
                {
                    Itemid = result.Read<uint>(1),
                    Count = result.Read<byte>(2),
                    LootListId = result.Read<uint>(3),
                    FollowLootRules = result.Read<bool>(4),
                    Freeforall = result.Read<bool>(5),
                    IsBlocked = result.Read<bool>(6),
                    IsCounted = result.Read<bool>(7),
                    IsUnderthreshold = result.Read<bool>(8),
                    NeedsQuest = result.Read<bool>(9),
                    RandomBonusListId = result.Read<uint>(10),
                    Context = (ItemContext)result.Read<byte>(11)
                };

                StringArray bonusLists = new(result.Read<string>(12), ' ');

                if (!bonusLists.IsEmpty())
                    foreach (string str in bonusLists)
                        lootItem.BonusListIDs.Add(uint.Parse(str));

                storedContainer.AddLootItem(lootItem, null);

                ++count;
            } while (result.NextRow());

            Log.Logger.Information($"Loaded {count} stored item loots in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }
        else
            Log.Logger.Information("Loaded 0 stored item loots");

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_ITEMCONTAINER_MONEY);
        result = _characterDatabase.Query(stmt);

        if (!result.IsEmpty())
        {
            count = 0;

            do
            {
                var key = result.Read<ulong>(0);

                if (!_lootItemStorage.ContainsKey(key))
                    _lootItemStorage.TryAdd(key, new StoredLootContainer(key, _characterDatabase));

                var storedContainer = _lootItemStorage[key];
                storedContainer.AddMoney(result.Read<uint>(1), null);

                ++count;
            } while (result.NextRow());

            Log.Logger.Information($"Loaded {count} stored item money in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }
        else
            Log.Logger.Information("Loaded 0 stored item money");
    }

    public bool LoadStoredLoot(Item item, Player player)
    {
        if (!_lootItemStorage.ContainsKey(item.GUID.Counter))
            return false;

        var container = _lootItemStorage[item.GUID.Counter];

        var loot = _lootFactory.GenerateLoot(player.Location.Map, item.GUID, LootType.Item);
        loot.Gold = container.GetMoney();

        var lt = _lootStorage.Items.GetLootFor(item.Entry);

        if (lt != null)
            foreach (var (id, storedItem) in container.GetLootItems().KeyValueList)
            {
                LootItem li = new(_objectManager, _conditionManager)
                {
                    Itemid = id,
                    Count = (byte)storedItem.Count,
                    LootListId = storedItem.ItemIndex,
                    FollowLootRules = storedItem.FollowRules,
                    Freeforall = storedItem.FFA,
                    IsBlocked = storedItem.Blocked,
                    IsCounted = storedItem.Counted,
                    IsUnderthreshold = storedItem.UnderThreshold,
                    NeedsQuest = storedItem.NeedsQuest,
                    RandomBonusListId = storedItem.RandomBonusListId,
                    Context = storedItem.Context,
                    BonusListIDs = storedItem.BonusListIDs
                };

                // Copy the extra loot conditions from the item in the loot template
                lt.CopyConditions(li);

                // If container item is in a bag, add that player as an allowed looter
                if (item.BagSlot != 0)
                    li.AddAllowedLooter(player);

                // Finally add the LootItem to the container
                loot.Items.Add(li);

                // Increment unlooted count
                ++loot.UnlootedCount;
            }

        // Mark the item if it has loot so it won't be generated again on open
        item.Loot = loot;
        item.LootGenerated = true;

        return true;
    }

    public void RemoveStoredLootForContainer(ulong containerId)
    {
        _lootItemStorage.TryRemove(containerId, out _);

        SQLTransaction trans = new();
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEMS);
        stmt.AddValue(0, containerId);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_MONEY);
        stmt.AddValue(0, containerId);
        trans.Append(stmt);

        _characterDatabase.CommitTransaction(trans);
    }

    public void RemoveStoredLootItemForContainer(ulong containerId, uint itemId, uint count, uint itemIndex)
    {
        if (!_lootItemStorage.ContainsKey(containerId))
            return;

        _lootItemStorage[containerId].RemoveItem(itemId, count, itemIndex);
    }

    public void RemoveStoredMoneyForContainer(ulong containerId)
    {
        if (!_lootItemStorage.ContainsKey(containerId))
            return;

        _lootItemStorage[containerId].RemoveMoney();
    }
}