// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Text;
using Framework.Database;

namespace Forged.MapServer.LootManagement;

internal class StoredLootContainer
{
    private readonly MultiMap<uint, StoredLootItem> _lootItems = new();
    private readonly ulong _containerId;
    private readonly CharacterDatabase _characterDatabase;
    private uint _money;

    public StoredLootContainer(ulong containerId, CharacterDatabase characterDatabase)
    {
        _containerId = containerId;
        _characterDatabase = characterDatabase;
    }

    public void AddLootItem(LootItem lootItem, SQLTransaction trans)
    {
        _lootItems.Add(lootItem.Itemid, new StoredLootItem(lootItem));

        if (trans == null)
            return;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_ITEMCONTAINER_ITEMS);

        // container_id, item_id, item_count, follow_rules, ffa, blocked, counted, under_threshold, needs_quest, rnd_prop, rnd_suffix
        stmt.AddValue(0, _containerId);
        stmt.AddValue(1, lootItem.Itemid);
        stmt.AddValue(2, lootItem.Count);
        stmt.AddValue(3, lootItem.LootListId);
        stmt.AddValue(4, lootItem.FollowLootRules);
        stmt.AddValue(5, lootItem.Freeforall);
        stmt.AddValue(6, lootItem.IsBlocked);
        stmt.AddValue(7, lootItem.IsCounted);
        stmt.AddValue(8, lootItem.IsUnderthreshold);
        stmt.AddValue(9, lootItem.NeedsQuest);
        stmt.AddValue(10, lootItem.RandomBonusListId);
        stmt.AddValue(11, (uint)lootItem.Context);

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

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_MONEY);
        stmt.AddValue(0, _containerId);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_ITEMCONTAINER_MONEY);
        stmt.AddValue(0, _containerId);
        stmt.AddValue(1, _money);
        trans.Append(stmt);
    }

    public void RemoveMoney()
    {
        _money = 0;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_MONEY);
        stmt.AddValue(0, _containerId);
        _characterDatabase.Execute(stmt);
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
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEM);
        stmt.AddValue(0, _containerId);
        stmt.AddValue(1, itemId);
        stmt.AddValue(2, count);
        stmt.AddValue(3, itemIndex);
        _characterDatabase.Execute(stmt);
    }

    public uint GetMoney()
    {
        return _money;
    }

    public MultiMap<uint, StoredLootItem> GetLootItems()
    {
        return _lootItems;
    }
}