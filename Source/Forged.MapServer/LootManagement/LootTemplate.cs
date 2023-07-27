// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Conditions;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals.Caching;
using Framework.Constants;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.LootManagement;

public class LootTemplate
{
    private readonly ConditionManager _conditionManager;
    private readonly IConfiguration _configuration;

    private readonly List<LootStoreItem> _entries = new();

    // not grouped only
    private readonly Dictionary<int, LootGroup> _groups = new();

    private readonly LootStoreBox _lootStorage;
    private readonly ItemTemplateCache _itemTemplateCache;
    private readonly ClassFactory _classFactory;
    
    // groups have own (optimised) processing, grouped entries go there

    public LootTemplate(IConfiguration configuration, ConditionManager conditionManager, LootStoreBox lootStorage, ItemTemplateCache itemTemplateCache, ClassFactory classFactory)
    {
        _configuration = configuration;
        _conditionManager = conditionManager;
        _lootStorage = lootStorage;
        _itemTemplateCache = itemTemplateCache;
        _classFactory = classFactory;
    }

    public bool AddConditionItem(Condition cond)
    {
        if (cond == null || !cond.IsLoaded()) //should never happen, checked at loading
        {
            Log.Logger.Error("LootTemplate.addConditionItem: condition is null");

            return false;
        }

        if (!_entries.Empty())
            foreach (var i in _entries.Where(i => i.Itemid == cond.SourceEntry))
            {
                i.Conditions.Add(cond);

                return true;
            }

        if (_groups.Empty())
            return false;

        {
            foreach (var group in _groups.Values)
            {
                if (group == null)
                    continue;

                var itemList = group.GetExplicitlyChancedItemList();

                if (!itemList.Empty())
                    foreach (var i in itemList.Where(i => i.Itemid == cond.SourceEntry))
                    {
                        i.Conditions.Add(cond);

                        return true;
                    }

                itemList = group.GetEqualChancedItemList();

                if (itemList.Empty())
                    continue;

                {
                    foreach (var i in itemList.Where(i => i.Itemid == cond.SourceEntry))
                    {
                        i.Conditions.Add(cond);

                        return true;
                    }
                }
            }
        }

        return false;
    }

    public void AddEntry(LootStoreItem item)
    {
        if (item.Groupid > 0 && item.Reference == 0) // Group
        {
            if (!_groups.ContainsKey(item.Groupid - 1))
                _groups[item.Groupid - 1] = _classFactory.Resolve<LootGroup>();

            _groups[item.Groupid - 1].AddEntry(item); // Adds new entry to the group
        }
        else // Non-grouped entries and references are stored together
            _entries.Add(item);
    }

    public void CheckLootRefs(Dictionary<uint, LootTemplate> store, List<uint> refSet)
    {
        foreach (var item in _entries.Where(item => item.Reference > 0))
            if (_lootStorage.Reference.GetLootFor(item.Reference) == null)
                _lootStorage.Reference.ReportNonExistingId(item.Reference, item.Itemid);
            else
                refSet?.Remove(item.Reference);

        foreach (var group in _groups.Values)
            group.CheckLootRefs(store, refSet);
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
        foreach (var item in _entries.Where(item => item.Itemid == li.Itemid))
        {
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

            return _groups[groupId - 1] != null && _groups[groupId - 1].HasQuestDrop();
        }

        foreach (var item in _entries)
            if (item.Reference > 0) // References
            {
                if (!store.TryGetValue(item.Reference, out var referenced))
                    continue; // Error message [should be] already printed at loading stage

                if (referenced.HasQuestDrop(store, item.Groupid))
                    return true;
            }
            else if (item.NeedsQuest)
                return true; // quest drop found

        // Now processing groups
        return _groups.Values.Any(group => group.HasQuestDrop());
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
                if (!store.TryGetValue(item.Reference, out var referenced))
                    continue; // Error message already printed at loading stage

                if (referenced.HasQuestDropForPlayer(store, player, item.Groupid))
                    return true;
            }
            else if (player.HasQuestForItem(item.Itemid))
                return true; // active quest drop found

        // Now checking groups
        return _groups.Values.Any(group => group.HasQuestDropForPlayer(player));
    }

    public bool IsReference(uint id)
    {
        return _entries.Any(storeItem => storeItem.Itemid == id && storeItem.Reference > 0);
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

                var maxcount = (uint)(item.Maxcount * _configuration.GetDefaultValue("Rate:Drop:Item:ReferencedAmount", 1.0f));

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
                                              !item.NeedsQuest || _itemTemplateCache.GetItemTemplate(item.Itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
                                              true,
                                              item.Conditions,
                                              _itemTemplateCache,
                                              _conditionManager))
                    loot.AddItem(item);
            }
        }

        // Now processing groups
        foreach (var group in _groups.Values)
            group?.Process(loot, lootMode, personalLooter);
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

                var maxcount = (uint)(item.Maxcount * _configuration.GetDefaultValue("Rate:Drop:Item:ReferencedAmount", 1.0f));
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
                var lootersForItem = GetLootersForItem(looter => LootItem.AllowedForPlayer(looter,
                                                                                           null,
                                                                                           item.Itemid,
                                                                                           item.NeedsQuest,
                                                                                           !item.NeedsQuest || _itemTemplateCache.GetItemTemplate(item.Itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
                                                                                           true,
                                                                                           item.Conditions,
                                                                                           _itemTemplateCache,
                                                                                           _conditionManager));

                if (lootersForItem.Empty())
                    continue;

                var chosenLooter = lootersForItem.SelectRandom();
                personalLoot[chosenLooter].AddItem(item);
            }
        }

        // Now processing groups
        foreach (var group in _groups.Values)
            if (group != null)
            {
                var lootersForGroup = GetLootersForItem(looter => group.HasDropForPlayer(looter, true));

                if (lootersForGroup.Empty())
                    continue;

                var chosenLooter = lootersForGroup.SelectRandom();
                group.Process(personalLoot[chosenLooter], lootMode);
            }
    }

    public void Verify(LootStore lootstore, uint id)
    {
        // Checking group chances
        foreach (var group in _groups)
            group.Value.Verify(lootstore, id, (byte)(group.Key + 1));

        // @todo References validity checks
    }

    // True if template includes at least 1 drop for the player
    private bool HasDropForPlayer(Player player, byte groupId, bool strictUsabilityCheck)
    {
        if (groupId != 0) // Group reference
        {
            if (groupId > _groups.Count)
                return false; // Error message already printed at loading stage

            return _groups[groupId - 1] != null && _groups[groupId - 1].HasDropForPlayer(player, strictUsabilityCheck);
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
                                               !lootStoreItem.NeedsQuest || _itemTemplateCache.GetItemTemplate(lootStoreItem.Itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
                                               strictUsabilityCheck,
                                               lootStoreItem.Conditions,
                                               _itemTemplateCache,
                                               _conditionManager))
                return true; // active quest drop found

        // Now checking groups
        return _groups.Values.Any(group => group != null && group.HasDropForPlayer(player, strictUsabilityCheck));
    }

    public class LootGroup // A set of loot definitions for items (refs are not allowed)
    {
        private readonly ConditionManager _conditionManager;
        private readonly List<LootStoreItem> _equalChanced = new();
        private readonly List<LootStoreItem> _explicitlyChanced = new();
        private readonly LootStoreBox _lootStorage;
        private readonly ItemTemplateCache _itemTemplateCache;
        
        // Entries with chances defined in DB
        // Zero chances - every entry takes the same chance

        public LootGroup(ConditionManager conditionManager, LootStoreBox lootStorage, ItemTemplateCache itemTemplateCache)
        {
            _conditionManager = conditionManager;
            _lootStorage = lootStorage;
            _itemTemplateCache = itemTemplateCache;
        }

        public void AddEntry(LootStoreItem item)
        {
            if (item.Chance != 0)
                _explicitlyChanced.Add(item);
            else
                _equalChanced.Add(item);
        }

        public void CheckLootRefs(Dictionary<uint, LootTemplate> store, List<uint> refSet)
        {
            foreach (var item in _explicitlyChanced.Where(item => item.Reference > 0))
                if (_lootStorage.Reference.GetLootFor(item.Reference) == null)
                    _lootStorage.Reference.ReportNonExistingId(item.Reference, item.Itemid);
                else
                    refSet?.Remove(item.Reference);

            foreach (var item in _equalChanced.Where(item => item.Reference > 0))
                if (_lootStorage.Reference.GetLootFor(item.Reference) == null)
                    _lootStorage.Reference.ReportNonExistingId(item.Reference, item.Itemid);
                else
                    refSet?.Remove(item.Reference);
        }

        public void CopyConditions(List<Condition> conditions)
        {
            foreach (var i in _explicitlyChanced)
                i.Conditions.Clear();

            foreach (var i in _equalChanced)
                i.Conditions.Clear();
        }

        public List<LootStoreItem> GetEqualChancedItemList()
        {
            return _equalChanced;
        }

        public List<LootStoreItem> GetExplicitlyChancedItemList()
        {
            return _explicitlyChanced;
        }

        public bool HasDropForPlayer(Player player, bool strictUsabilityCheck)
        {
            if (_explicitlyChanced.Any(lootStoreItem => LootItem.AllowedForPlayer(player,
                                                                                  null,
                                                                                  lootStoreItem.Itemid,
                                                                                  lootStoreItem.NeedsQuest,
                                                                                  !lootStoreItem.NeedsQuest || _itemTemplateCache.GetItemTemplate(lootStoreItem.Itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
                                                                                  strictUsabilityCheck,
                                                                                  lootStoreItem.Conditions,
                                                                                  _itemTemplateCache,
                                                                                  _conditionManager)))
                return true;

            return _equalChanced.Any(lootStoreItem => LootItem.AllowedForPlayer(player,
                                                                                null,
                                                                                lootStoreItem.Itemid,
                                                                                lootStoreItem.NeedsQuest,
                                                                                !lootStoreItem.NeedsQuest || _itemTemplateCache.GetItemTemplate(lootStoreItem.Itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
                                                                                strictUsabilityCheck,
                                                                                lootStoreItem.Conditions,
                                                                                _itemTemplateCache,
                                                                                _conditionManager));
        }

        public bool HasQuestDrop()
        {
            return _explicitlyChanced.Any(i => i.NeedsQuest) || _equalChanced.Any(i => i.NeedsQuest);
        }

        public bool HasQuestDropForPlayer(Player player)
        {
            return _explicitlyChanced.Any(i => player.HasQuestForItem(i.Itemid)) || _equalChanced.Any(i => player.HasQuestForItem(i.Itemid));
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
                Log.Logger.Error("Table '{0}' entry {1} group {2} has total chance > 100% ({3})", lootstore.Name, id, groupID, chance);

            if (chance >= 100.0f && !_equalChanced.Empty())
                Log.Logger.Error("Table '{0}' entry {1} group {2} has items with chance=0% but group total chance >= 100% ({3})", lootstore.Name, id, groupID, chance);
        }

        private float RawTotalChance()
        {
            return _explicitlyChanced.Where(i => !i.NeedsQuest).Sum(i => i.Chance);
        }

        private LootStoreItem Roll(ushort lootMode, Player personalLooter = null)
        {
            var possibleLoot = _explicitlyChanced;
            possibleLoot.RemoveAll(new LootGroupInvalidSelector(lootMode, personalLooter, _itemTemplateCache, _conditionManager).Check);

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
            possibleLoot.RemoveAll(new LootGroupInvalidSelector(lootMode, personalLooter, _itemTemplateCache, _conditionManager).Check);

            return !possibleLoot.Empty()
                       ? // If nothing selected yet - an item is taken from equal-chanced part
                       possibleLoot.SelectRandom()
                       : null; // Empty drop from the group
        }
    }
}