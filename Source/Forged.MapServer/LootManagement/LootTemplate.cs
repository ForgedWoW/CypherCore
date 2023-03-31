// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.LootManagement;

public class LootTemplate
{
    private readonly IConfiguration _configuration;
    private readonly GameObjectManager _objectManager;
    private readonly ConditionManager _conditionManager;
    private readonly LootStoreBox _lootStorage;
    private readonly List<LootStoreItem> _entries = new();       // not grouped only
    private readonly Dictionary<int, LootGroup> _groups = new(); // groups have own (optimised) processing, grouped entries go there

    public LootTemplate(IConfiguration configuration, GameObjectManager objectManager, ConditionManager conditionManager, LootStoreBox lootStorage)
    {
        _configuration = configuration;
        _objectManager = objectManager;
        _conditionManager = conditionManager;
        _lootStorage = lootStorage;
    }

    public void AddEntry(LootStoreItem item)
    {
        if (item.Groupid > 0 && item.Reference == 0) // Group
        {
            if (!_groups.ContainsKey(item.Groupid - 1))
                _groups[item.Groupid - 1] = new LootGroup(_objectManager, _conditionManager, _lootStorage);

            _groups[item.Groupid - 1].AddEntry(item); // Adds new entry to the group
        }
        else // Non-grouped entries and references are stored together
        {
            _entries.Add(item);
        }
    }

    public void Process(Loot loot, bool rate, ushort lootMode, byte groupId, Player personalLooter = null)
    {
        if (groupId != 0) // Group reference uses own processing of the group
        {
            if (groupId > _groups.Count)
                return; // Error message already printed at loading stage

            if (_groups[groupId - 1] == null)
                return;

            _groups[groupId - 1].Process(loot, lootMode, personalLooter);

            return;
        }

        // Rolling non-grouped items
        foreach (var item in _entries)
        {
            if (!Convert.ToBoolean(item.Lootmode & lootMode)) // Do not add if mode mismatch
                continue;

            if (!item.Roll(rate))
                continue; // Bad luck for the entry

            if (item.Reference > 0) // References processing
            {
                var referenced = _lootStorage.Reference.GetLootFor(item.Reference);

                if (referenced == null)
                    continue; // Error message already printed at loading stage

                var maxcount = (uint)(item.Maxcount * _configuration.GetDefaultValue("Rate.Drop.Item.ReferencedAmount", 1.0f));

                for (uint loop = 0; loop < maxcount; ++loop) // Ref multiplicator
                    referenced.Process(loot, rate, lootMode, item.Groupid, personalLooter);
            }
            else
            {
                // Plain entries (not a reference, not grouped)
                // Chance is already checked, just add
                if (personalLooter == null ||
                    LootItem.AllowedForPlayer(personalLooter,
                                              null,
                                              item.Itemid,
                                              item.NeedsQuest,
                                              !item.NeedsQuest || _objectManager.GetItemTemplate(item.Itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
                                              true,
                                              item.Conditions,
                                              _objectManager,
                                              _conditionManager))
                    loot.AddItem(item);
            }
        }

        // Now processing groups
        foreach (var group in _groups.Values)
            if (group != null)
                group.Process(loot, lootMode, personalLooter);
    }

    public void ProcessPersonalLoot(Dictionary<Player, Loot> personalLoot, bool rate, ushort lootMode)
    {
        List<Player> GetLootersForItem(Func<Player, bool> predicate)
        {
            List<Player> lootersForItem = new();

            foreach (var (looter, _) in personalLoot)
                if (predicate(looter))
                    lootersForItem.Add(looter);

            return lootersForItem;
        }

        // Rolling non-grouped items
        foreach (var item in _entries)
        {
            if ((item.Lootmode & lootMode) == 0) // Do not add if mode mismatch
                continue;

            if (!item.Roll(rate))
                continue; // Bad luck for the entry

            if (item.Reference > 0) // References processing
            {
                var referenced = _lootStorage.Reference.GetLootFor(item.Reference);

                if (referenced == null)
                    continue; // Error message already printed at loading stage

                var maxcount = (uint)(item.Maxcount * _configuration.GetDefaultValue("Rate.Drop.Item.ReferencedAmount", 1.0f));
                List<Player> gotLoot = new();

                for (uint loop = 0; loop < maxcount; ++loop) // Ref multiplicator
                {
                    var lootersForItem = GetLootersForItem(looter => referenced.HasDropForPlayer(looter, item.Groupid, true));

                    // nobody can loot this, skip it
                    if (lootersForItem.Empty())
                        break;

                    var newEnd = lootersForItem.RemoveAll(looter => gotLoot.Contains(looter));

                    if (lootersForItem.Count == newEnd)
                        // if we run out of looters this means that there are more items dropped than players
                        // start a new cycle adding one item to everyone
                        gotLoot.Clear();
                    else
                        lootersForItem.RemoveRange(newEnd, lootersForItem.Count - newEnd);

                    var chosenLooter = lootersForItem.SelectRandom();
                    referenced.Process(personalLoot[chosenLooter], rate, lootMode, item.Groupid, chosenLooter);
                    gotLoot.Add(chosenLooter);
                }
            }
            else
            {
                // Plain entries (not a reference, not grouped)
                // Chance is already checked, just add
                var lootersForItem = GetLootersForItem(looter =>
                {
                    return LootItem.AllowedForPlayer(looter,
                                                     null,
                                                     item.Itemid,
                                                     item.NeedsQuest,
                                                     !item.NeedsQuest || _objectManager.GetItemTemplate(item.Itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
                                                     true,
                                                     item.Conditions,
                                                     _objectManager,
                                                     _conditionManager);
                });

                if (!lootersForItem.Empty())
                {
                    var chosenLooter = lootersForItem.SelectRandom();
                    personalLoot[chosenLooter].AddItem(item);
                }
            }
        }

        // Now processing groups
        foreach (var group in _groups.Values)
            if (group != null)
            {
                var lootersForGroup = GetLootersForItem(looter => group.HasDropForPlayer(looter, true));

                if (!lootersForGroup.Empty())
                {
                    var chosenLooter = lootersForGroup.SelectRandom();
                    group.Process(personalLoot[chosenLooter], lootMode);
                }
            }
    }

    public void CopyConditions(List<Condition> conditions)
    {
        foreach (var i in _entries)
            i.Conditions.Clear();

        foreach (var group in _groups.Values)
            group.CopyConditions(conditions);
    }

    public void CopyConditions(LootItem li)
    {
        // Copies the conditions list from a template item to a LootItem
        foreach (var item in _entries)
        {
            if (item.Itemid != li.Itemid)
                continue;

            li.Conditions = item.Conditions;

            break;
        }
    }

    public bool HasQuestDrop(Dictionary<uint, LootTemplate> store, byte groupId = 0)
    {
        if (groupId != 0) // Group reference
        {
            if (groupId > _groups.Count)
                return false; // Error message [should be] already printed at loading stage

            if (_groups[groupId - 1] == null)
                return false;

            return _groups[groupId - 1].HasQuestDrop();
        }

        foreach (var item in _entries)
            if (item.Reference > 0) // References
            {
                var referenced = store.LookupByKey(item.Reference);

                if (referenced == null)
                    continue; // Error message [should be] already printed at loading stage

                if (referenced.HasQuestDrop(store, item.Groupid))
                    return true;
            }
            else if (item.NeedsQuest)
            {
                return true; // quest drop found
            }

        // Now processing groups
        foreach (var group in _groups.Values)
            if (group.HasQuestDrop())
                return true;

        return false;
    }

    public bool HasQuestDropForPlayer(Dictionary<uint, LootTemplate> store, Player player, byte groupId = 0)
    {
        if (groupId != 0) // Group reference
        {
            if (groupId > _groups.Count)
                return false; // Error message already printed at loading stage

            if (_groups[groupId - 1] == null)
                return false;

            return _groups[groupId - 1].HasQuestDropForPlayer(player);
        }

        // Checking non-grouped entries
        foreach (var item in _entries)
            if (item.Reference > 0) // References processing
            {
                var referenced = store.LookupByKey(item.Reference);

                if (referenced == null)
                    continue; // Error message already printed at loading stage

                if (referenced.HasQuestDropForPlayer(store, player, item.Groupid))
                    return true;
            }
            else if (player.HasQuestForItem(item.Itemid))
            {
                return true; // active quest drop found
            }

        // Now checking groups
        foreach (var group in _groups.Values)
            if (group.HasQuestDropForPlayer(player))
                return true;

        return false;
    }

    public void Verify(LootStore lootstore, uint id)
    {
        // Checking group chances
        foreach (var group in _groups)
            group.Value.Verify(lootstore, id, (byte)(group.Key + 1));

        // @todo References validity checks
    }

    public void CheckLootRefs(Dictionary<uint, LootTemplate> store, List<uint> refSet)
    {
        foreach (var item in _entries)
            if (item.Reference > 0)
            {
                if (_lootStorage.Reference.GetLootFor(item.Reference) == null)
                    _lootStorage.Reference.ReportNonExistingId(item.Reference, item.Itemid);
                else if (refSet != null)
                    refSet.Remove(item.Reference);
            }

        foreach (var group in _groups.Values)
            group.CheckLootRefs(store, refSet);
    }

    public bool AddConditionItem(Condition cond)
    {
        if (cond == null || !cond.IsLoaded()) //should never happen, checked at loading
        {
            Log.Logger.Error("LootTemplate.addConditionItem: condition is null");

            return false;
        }

        if (!_entries.Empty())
            foreach (var i in _entries)
                if (i.Itemid == cond.SourceEntry)
                {
                    i.Conditions.Add(cond);

                    return true;
                }

        if (!_groups.Empty())
            foreach (var group in _groups.Values)
            {
                if (group == null)
                    continue;

                var itemList = group.GetExplicitlyChancedItemList();

                if (!itemList.Empty())
                    foreach (var i in itemList)
                        if (i.Itemid == cond.SourceEntry)
                        {
                            i.Conditions.Add(cond);

                            return true;
                        }

                itemList = group.GetEqualChancedItemList();

                if (!itemList.Empty())
                    foreach (var i in itemList)
                        if (i.Itemid == cond.SourceEntry)
                        {
                            i.Conditions.Add(cond);

                            return true;
                        }
            }

        return false;
    }

    public bool IsReference(uint id)
    {
        foreach (var storeItem in _entries)
            if (storeItem.Itemid == id && storeItem.Reference > 0)
                return true;

        return false; //not found or not reference
    }

    // True if template includes at least 1 drop for the player
    private bool HasDropForPlayer(Player player, byte groupId, bool strictUsabilityCheck)
    {
        if (groupId != 0) // Group reference
        {
            if (groupId > _groups.Count)
                return false; // Error message already printed at loading stage

            if (_groups[groupId - 1] == null)
                return false;

            return _groups[groupId - 1].HasDropForPlayer(player, strictUsabilityCheck);
        }

        // Checking non-grouped entries
        foreach (var lootStoreItem in _entries)
            if (lootStoreItem.Reference > 0) // References processing
            {
                var referenced = _lootStorage.Reference.GetLootFor(lootStoreItem.Reference);

                if (referenced == null)
                    continue; // Error message already printed at loading stage

                if (referenced.HasDropForPlayer(player, lootStoreItem.Groupid, strictUsabilityCheck))
                    return true;
            }
            else if (LootItem.AllowedForPlayer(player,
                                               null,
                                               lootStoreItem.Itemid,
                                               lootStoreItem.NeedsQuest,
                                               !lootStoreItem.NeedsQuest || _objectManager.GetItemTemplate(lootStoreItem.Itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
                                               strictUsabilityCheck,
                                               lootStoreItem.Conditions,
                                               _objectManager,
                                               _conditionManager))
            {
                return true; // active quest drop found
            }

        // Now checking groups
        foreach (var group in _groups.Values)
            if (group != null && group.HasDropForPlayer(player, strictUsabilityCheck))
                return true;

        return false;
    }

    public class LootGroup // A set of loot definitions for items (refs are not allowed)
    {
        private readonly GameObjectManager _objectManager;
        private readonly ConditionManager _conditionManager;
        private readonly LootStoreBox _lootStorage;
        private readonly List<LootStoreItem> _explicitlyChanced = new(); // Entries with chances defined in DB
        private readonly List<LootStoreItem> _equalChanced = new();      // Zero chances - every entry takes the same chance

        public LootGroup(GameObjectManager gameObjectManager, ConditionManager conditionManager, LootStoreBox lootStorage)
        {
            _objectManager = gameObjectManager;
            _conditionManager = conditionManager;
            _lootStorage = lootStorage;
        }

        public void AddEntry(LootStoreItem item)
        {
            if (item.Chance != 0)
                _explicitlyChanced.Add(item);
            else
                _equalChanced.Add(item);
        }

        public bool HasQuestDrop()
        {
            foreach (var i in _explicitlyChanced)
                if (i.NeedsQuest)
                    return true;

            foreach (var i in _equalChanced)
                if (i.NeedsQuest)
                    return true;

            return false;
        }

        public bool HasQuestDropForPlayer(Player player)
        {
            foreach (var i in _explicitlyChanced)
                if (player.HasQuestForItem(i.Itemid))
                    return true;

            foreach (var i in _equalChanced)
                if (player.HasQuestForItem(i.Itemid))
                    return true;

            return false;
        }

        public void Process(Loot loot, ushort lootMode, Player personalLooter = null)
        {
            var item = Roll(lootMode, personalLooter);

            if (item != null)
                loot.AddItem(item);
        }

        public void Verify(LootStore lootstore, uint id, byte groupID = 0)
        {
            var chance = RawTotalChance();

            if (chance > 101.0f) // @todo replace with 100% when DBs will be ready
                Log.Logger.Error("Table '{0}' entry {1} group {2} has total chance > 100% ({3})", lootstore.GetName(), id, groupID, chance);

            if (chance >= 100.0f && !_equalChanced.Empty())
                Log.Logger.Error("Table '{0}' entry {1} group {2} has items with chance=0% but group total chance >= 100% ({3})", lootstore.GetName(), id, groupID, chance);
        }

        public void CheckLootRefs(Dictionary<uint, LootTemplate> store, List<uint> refSet)
        {
            foreach (var item in _explicitlyChanced)
                if (item.Reference > 0)
                {
                    if (_lootStorage.Reference.GetLootFor(item.Reference) == null)
                        _lootStorage.Reference.ReportNonExistingId(item.Reference, item.Itemid);
                    else if (refSet != null)
                        refSet.Remove(item.Reference);
                }

            foreach (var item in _equalChanced)
                if (item.Reference > 0)
                {
                    if (_lootStorage.Reference.GetLootFor(item.Reference) == null)
                        _lootStorage.Reference.ReportNonExistingId(item.Reference, item.Itemid);
                    else if (refSet != null)
                        refSet.Remove(item.Reference);
                }
        }

        public List<LootStoreItem> GetExplicitlyChancedItemList()
        {
            return _explicitlyChanced;
        }

        public List<LootStoreItem> GetEqualChancedItemList()
        {
            return _equalChanced;
        }

        public void CopyConditions(List<Condition> conditions)
        {
            foreach (var i in _explicitlyChanced)
                i.Conditions.Clear();

            foreach (var i in _equalChanced)
                i.Conditions.Clear();
        }

        public bool HasDropForPlayer(Player player, bool strictUsabilityCheck)
        {
            foreach (var lootStoreItem in _explicitlyChanced)
                if (LootItem.AllowedForPlayer(player,
                                              null,
                                              lootStoreItem.Itemid,
                                              lootStoreItem.NeedsQuest,
                                              !lootStoreItem.NeedsQuest || _objectManager.GetItemTemplate(lootStoreItem.Itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
                                              strictUsabilityCheck,
                                              lootStoreItem.Conditions,
                                              _objectManager,
                                              _conditionManager))
                    return true;

            foreach (var lootStoreItem in _equalChanced)
                if (LootItem.AllowedForPlayer(player,
                                              null,
                                              lootStoreItem.Itemid,
                                              lootStoreItem.NeedsQuest,
                                              !lootStoreItem.NeedsQuest || _objectManager.GetItemTemplate(lootStoreItem.Itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
                                              strictUsabilityCheck,
                                              lootStoreItem.Conditions,
                                              _objectManager,
                                              _conditionManager))
                    return true;

            return false;
        }

        private float RawTotalChance()
        {
            float result = 0;

            foreach (var i in _explicitlyChanced)
                if (!i.NeedsQuest)
                    result += i.Chance;

            return result;
        }

        private LootStoreItem Roll(ushort lootMode, Player personalLooter = null)
        {
            var possibleLoot = _explicitlyChanced;
            possibleLoot.RemoveAll(new LootGroupInvalidSelector(lootMode, personalLooter, _objectManager, _conditionManager).Check);

            if (!possibleLoot.Empty()) // First explicitly chanced entries are checked
            {
                var roll = (float)RandomHelper.randChance();

                foreach (var item in possibleLoot) // check each explicitly chanced entry in the template and modify its chance based on quality.
                {
                    if (item.Chance >= 100.0f)
                        return item;

                    roll -= item.Chance;

                    if (roll < 0)
                        return item;
                }
            }

            possibleLoot = _equalChanced;
            possibleLoot.RemoveAll(new LootGroupInvalidSelector(lootMode, personalLooter, _objectManager, _conditionManager).Check);

            if (!possibleLoot.Empty()) // If nothing selected yet - an item is taken from equal-chanced part
                return possibleLoot.SelectRandom();

            return null; // Empty drop from the group
        }
    }
}