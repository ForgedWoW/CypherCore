// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.Entities.Players;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class AccessRequirementsCache : IObjectCache
{
    private readonly GameObjectManager _gameObjectManager;
    private readonly DB6Storage<MapRecord> _mapRecords;
    private readonly DB2Manager _db2Manager;
    private readonly WorldDatabase _worldDatabase;
    private readonly DB6Storage<AchievementRecord> _achievementRecords;
    private readonly ItemTemplateCache _itemTemplateCache;
    private readonly Dictionary<ulong, AccessRequirement> _accessRequirementStorage = new();

    public AccessRequirementsCache(GameObjectManager gameObjectManager, DB6Storage<MapRecord> mapRecords, DB2Manager db2Manager, WorldDatabase worldDatabase, 
                                   DB6Storage<AchievementRecord> achievementRecords, ItemTemplateCache itemTemplateCache)
    {
        _gameObjectManager = gameObjectManager;
        _mapRecords = mapRecords;
        _db2Manager = db2Manager;
        _worldDatabase = worldDatabase;
        _achievementRecords = achievementRecords;
        _itemTemplateCache = itemTemplateCache;
    }

    public AccessRequirement GetAccessRequirement(uint mapid, Difficulty difficulty)
    {
        return _accessRequirementStorage.LookupByKey(MathFunctions.MakePair64(mapid, (uint)difficulty));
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        _accessRequirementStorage.Clear();

        //                                          0      1           2          3          4           5      6             7             8                      9
        var result = _worldDatabase.Query("SELECT mapid, difficulty, level_min, level_max, item, item2, quest_done_A, quest_done_H, completed_achievement, quest_failed_text FROM access_requirement");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 access requirement definitions. DB table `access_requirement` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var mapid = result.Read<uint>(0);

            if (!_mapRecords.ContainsKey(mapid))
            {
                Log.Logger.Error("Map {0} referenced in `access_requirement` does not exist, skipped.", mapid);

                continue;
            }

            var difficulty = result.Read<uint>(1);

            if (_db2Manager.GetMapDifficultyData(mapid, (Difficulty)difficulty) == null)
            {
                Log.Logger.Error("Map {0} referenced in `access_requirement` does not have difficulty {1}, skipped", mapid, difficulty);

                continue;
            }

            var requirementId = MathFunctions.MakePair64(mapid, difficulty);

            AccessRequirement ar = new()
            {
                LevelMin = result.Read<byte>(2),
                LevelMax = result.Read<byte>(3),
                Item = result.Read<uint>(4),
                Item2 = result.Read<uint>(5),
                QuestA = result.Read<uint>(6),
                QuestH = result.Read<uint>(7),
                Achievement = result.Read<uint>(8),
                QuestFailedText = result.Read<string>(9)
            };

            if (ar.Item != 0)
            {
                var pProto = _itemTemplateCache.GetItemTemplate(ar.Item);

                if (pProto == null)
                {
                    Log.Logger.Error("Key item {0} does not exist for map {1} difficulty {2}, removing key requirement.", ar.Item, mapid, difficulty);
                    ar.Item = 0;
                }
            }

            if (ar.Item2 != 0)
            {
                var pProto = _itemTemplateCache.GetItemTemplate(ar.Item2);

                if (pProto == null)
                {
                    Log.Logger.Error("Second item {0} does not exist for map {1} difficulty {2}, removing key requirement.", ar.Item2, mapid, difficulty);
                    ar.Item2 = 0;
                }
            }

            if (ar.QuestA != 0)
                if (_gameObjectManager.QuestTemplateCache.GetQuestTemplate(ar.QuestA) == null)
                {
                    Log.Logger.Error("Required Alliance QuestId {0} not exist for map {1} difficulty {2}, remove quest done requirement.", ar.QuestA, mapid, difficulty);
                    ar.QuestA = 0;
                }

            if (ar.QuestH != 0)
                if (_gameObjectManager.QuestTemplateCache.GetQuestTemplate(ar.QuestH) == null)
                {
                    Log.Logger.Error("Required Horde QuestId {0} not exist for map {1} difficulty {2}, remove quest done requirement.", ar.QuestH, mapid, difficulty);
                    ar.QuestH = 0;
                }

            if (ar.Achievement != 0)
                if (!_achievementRecords.ContainsKey(ar.Achievement))
                {
                    Log.Logger.Error("Required Achievement {0} not exist for map {1} difficulty {2}, remove quest done requirement.", ar.Achievement, mapid, difficulty);
                    ar.Achievement = 0;
                }

            _accessRequirementStorage[requirementId] = ar;
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} access requirement definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}