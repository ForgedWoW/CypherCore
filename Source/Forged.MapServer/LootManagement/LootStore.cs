// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.LootManagement;

public class LootStore
{
    private readonly ConditionManager _conditionManager;
    private readonly IConfiguration _configuration;
    private readonly string _entryName;
    private readonly Dictionary<uint, LootTemplate> _lootTemplates = new();
    private readonly string _name;
    private readonly GameObjectManager _objectManager;
    private readonly bool _ratesAllowed;
    private readonly LootStoreBox _storage;
    private readonly WorldDatabase _worldDatabase;
    public LootStore(IConfiguration configuration, WorldDatabase worldDatabase, ConditionManager conditionManager, GameObjectManager objectManager, LootStoreBox storage, string name, string entryName, bool ratesAllowed = true)
    {
        _configuration = configuration;
        _worldDatabase = worldDatabase;
        _conditionManager = conditionManager;
        _objectManager = objectManager;
        _storage = storage;
        _name = name;
        _entryName = entryName;
        _ratesAllowed = ratesAllowed;
    }

    public void CheckLootRefs(List<uint> refSet = null)
    {
        foreach (var pair in _lootTemplates)
            pair.Value.CheckLootRefs(_lootTemplates, refSet);
    }

    public LootTemplate GetLootFor(uint lootID)
    {
        var tab = _lootTemplates.LookupByKey(lootID);

        return tab;
    }

    public LootTemplate GetLootForConditionFill(uint lootID)
    {
        var tab = _lootTemplates.LookupByKey(lootID);

        return tab;
    }

    public string GetName()
    {
        return _name;
    }

    public bool HaveLootFor(uint lootID)
    {
        return _lootTemplates.LookupByKey(lootID) != null;
    }

    public bool HaveQuestLootFor(uint lootID)
    {
        if (!_lootTemplates.TryGetValue(lootID, out var lootTemplate))
            return false;

        // scan loot for quest items
        return lootTemplate.HasQuestDrop(_lootTemplates);
    }

    public bool HaveQuestLootForPlayer(uint lootID, Player player)
    {
        if (_lootTemplates.TryGetValue(lootID, out var tab))
            if (tab.HasQuestDropForPlayer(_lootTemplates, player))
                return true;

        return false;
    }

    public bool IsRatesAllowed()
    {
        return _ratesAllowed;
    }

    public uint LoadAndCollectLootIds(out List<uint> lootIdSet)
    {
        var count = LoadLootTable();
        lootIdSet = new List<uint>();

        foreach (var tab in _lootTemplates)
            lootIdSet.Add(tab.Key);

        return count;
    }
    public void ReportNonExistingId(uint lootId, uint ownerId)
    {
        Log.Logger.Debug("Table '{0}' Entry {1} does not exist but it is used by {2} {3}", GetName(), lootId, GetEntryName(), ownerId);
    }

    public void ReportUnusedIds(List<uint> lootIdSet)
    {
        // all still listed ids isn't referenced
        foreach (var id in lootIdSet)
            if (_configuration.GetDefaultValue("load.autoclean", false))
                _worldDatabase.Execute($"DELETE FROM {GetName()} WHERE Entry = {id}");
            else
                Log.Logger.Error("Table '{0}' entry {1} isn't {2} and not referenced from loot, and then useless.", GetName(), id, GetEntryName());
    }
    public void ResetConditions()
    {
        foreach (var pair in _lootTemplates)
        {
            List<Condition> empty = new();
            pair.Value.CopyConditions(empty);
        }
    }
    private void Clear()
    {
        _lootTemplates.Clear();
    }

    private string GetEntryName()
    {
        return _entryName;
    }

    private uint LoadLootTable()
    {
        // Clearing store (for reloading case)
        Clear();

        //                                            0     1      2        3         4             5          6        7         8
        var result = _worldDatabase.Query("SELECT Entry, Item, Reference, Chance, QuestRequired, LootMode, GroupId, MinCount, MaxCount FROM {0}", GetName());

        if (result.IsEmpty())
            return 0;

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);
            var item = result.Read<uint>(1);
            var reference = result.Read<uint>(2);
            var chance = result.Read<float>(3);
            var needsquest = result.Read<bool>(4);
            var lootmode = result.Read<ushort>(5);
            var groupid = result.Read<byte>(6);
            var mincount = result.Read<byte>(7);
            var maxcount = result.Read<byte>(8);

            if (groupid >= 1 << 7) // it stored in 7 bit field
            {
                Log.Logger.Error("Table '{0}' entry {1} item {2}: group ({3}) must be less {4} - skipped", GetName(), entry, item, groupid, 1 << 7);

                return 0;
            }

            LootStoreItem storeitem = new(item, reference, chance, needsquest, lootmode, groupid, mincount, maxcount, _objectManager, _configuration, _worldDatabase);

            if (!storeitem.IsValid(this, entry)) // Validity checks
                continue;

            // Looking for the template of the entry
            // often entries are put together
            if (_lootTemplates.Empty() || !_lootTemplates.ContainsKey(entry))
                _lootTemplates.Add(entry, new LootTemplate(_configuration, _objectManager, _conditionManager, _storage));

            // Adds current row to the template
            _lootTemplates[entry].AddEntry(storeitem);
            ++count;
        } while (result.NextRow());

        Verify(); // Checks validity of the loot store

        return count;
    }

    private void Verify()
    {
        foreach (var i in _lootTemplates)
            i.Value.Verify(this, i.Key);
    }
}