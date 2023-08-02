// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.F;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class FactionChangeCache : IObjectCache
{
    private readonly WorldDatabase _worldDatabase;
    private readonly ItemTemplateCache _itemTemplateCache;
    private readonly DB6Storage<AchievementRecord> _achievementRecords;
    private readonly QuestTemplateCache _questTemplateCache;
    private readonly DB6Storage<FactionRecord> _factionRecords;
    private readonly SpellManager _spellManager;

    public Dictionary<uint, uint> FactionChangeAchievements { get; set; } = new();
    public Dictionary<uint, uint> FactionChangeItemsAllianceToHorde { get; set; } = new();
    public Dictionary<uint, uint> FactionChangeItemsHordeToAlliance { get; set; } = new();
    public Dictionary<uint, uint> FactionChangeQuests { get; set; } = new();
    public Dictionary<uint, uint> FactionChangeReputation { get; set; } = new();
    public Dictionary<uint, uint> FactionChangeSpells { get; set; } = new();

    public FactionChangeCache(WorldDatabase worldDatabase, ItemTemplateCache itemTemplateCache, DB6Storage<AchievementRecord> achievementRecords,
                              QuestTemplateCache questTemplateCache, DB6Storage<FactionRecord> factionRecords, SpellManager spellManager)
    {
        _worldDatabase = worldDatabase;
        _itemTemplateCache = itemTemplateCache;
        _achievementRecords = achievementRecords;
        _questTemplateCache = questTemplateCache;
        _factionRecords = factionRecords;
        _spellManager = spellManager;
    }

    public void Load()
    {
        LoadFactionChangeAchievements();
        LoadFactionChangeItems();
        LoadFactionChangeQuests();
        LoadFactionChangeReputations();
        LoadFactionChangeSpells();
    }

    public void LoadFactionChangeAchievements()
    {
        var oldMSTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT alliance_id, horde_id FROM player_factionchange_achievement");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 faction change achievement pairs. DB table `player_factionchange_achievement` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var alliance = result.Read<uint>(0);
            var horde = result.Read<uint>(1);

            if (!_achievementRecords.ContainsKey(alliance))
                Log.Logger.Error("Achievement {0} (alliance_id) referenced in `player_factionchange_achievement` does not exist, pair skipped!", alliance);
            else if (!_achievementRecords.ContainsKey(horde))
                Log.Logger.Error("Achievement {0} (horde_id) referenced in `player_factionchange_achievement` does not exist, pair skipped!", horde);
            else
                FactionChangeAchievements[alliance] = horde;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} faction change achievement pairs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadFactionChangeItems()
    {
        var oldMSTime = Time.MSTime;

        uint count = 0;

        foreach (var itemPair in _itemTemplateCache.ItemTemplates)
        {
            if (itemPair.Value.OtherFactionItemId == 0)
                continue;

            if (itemPair.Value.HasFlag(ItemFlags2.FactionHorde))
                FactionChangeItemsHordeToAlliance[itemPair.Key] = itemPair.Value.OtherFactionItemId;

            if (itemPair.Value.HasFlag(ItemFlags2.FactionAlliance))
                FactionChangeItemsAllianceToHorde[itemPair.Key] = itemPair.Value.OtherFactionItemId;

            ++count;
        }

        Log.Logger.Information("Loaded {0} faction change item pairs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadFactionChangeQuests()
    {
        var oldMSTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT alliance_id, horde_id FROM player_factionchange_quests");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 faction change quest pairs. DB table `player_factionchange_quests` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var alliance = result.Read<uint>(0);
            var horde = result.Read<uint>(1);

            if (_questTemplateCache.GetQuestTemplate(alliance) == null)
                Log.Logger.Error("QuestId {0} (alliance_id) referenced in `player_factionchange_quests` does not exist, pair skipped!", alliance);
            else if (_questTemplateCache.GetQuestTemplate(horde) == null)
                Log.Logger.Error("QuestId {0} (horde_id) referenced in `player_factionchange_quests` does not exist, pair skipped!", horde);
            else
                FactionChangeQuests[alliance] = horde;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} faction change quest pairs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadFactionChangeReputations()
    {
        var oldMSTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT alliance_id, horde_id FROM player_factionchange_reputations");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 faction change reputation pairs. DB table `player_factionchange_reputations` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var alliance = result.Read<uint>(0);
            var horde = result.Read<uint>(1);

            if (!_factionRecords.ContainsKey(alliance))
                Log.Logger.Error("Reputation {0} (alliance_id) referenced in `player_factionchange_reputations` does not exist, pair skipped!", alliance);
            else if (!_factionRecords.ContainsKey(horde))
                Log.Logger.Error("Reputation {0} (horde_id) referenced in `player_factionchange_reputations` does not exist, pair skipped!", horde);
            else
                FactionChangeReputation[alliance] = horde;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} faction change reputation pairs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadFactionChangeSpells()
    {
        var oldMSTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT alliance_id, horde_id FROM player_factionchange_spells");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 faction change spell pairs. DB table `player_factionchange_spells` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var alliance = result.Read<uint>(0);
            var horde = result.Read<uint>(1);

            if (!_spellManager.HasSpellInfo(alliance))
                Log.Logger.Error("Spell {0} (alliance_id) referenced in `player_factionchange_spells` does not exist, pair skipped!", alliance);
            else if (!_spellManager.HasSpellInfo(horde))
                Log.Logger.Error("Spell {0} (horde_id) referenced in `player_factionchange_spells` does not exist, pair skipped!", horde);
            else
                FactionChangeSpells[alliance] = horde;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} faction change spell pairs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}