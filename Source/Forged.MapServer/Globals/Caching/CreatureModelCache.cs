// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.Entities.Creatures;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class CreatureModelCache : IObjectCache
{
    private readonly DB6Storage<CreatureDisplayInfoRecord> _creatureDisplayInfoRecords;
    private readonly DB6Storage<CreatureModelDataRecord> _creatureModelDataRecords;
    private readonly Dictionary<uint, CreatureModelInfo> _creatureModelStorage = new();
    private readonly WorldDatabase _worldDatabase;

    public CreatureModelCache(WorldDatabase worldDatabase, DB6Storage<CreatureDisplayInfoRecord> creatureDisplayInfoRecords, DB6Storage<CreatureModelDataRecord> creatureModelDataRecords)
    {
        _worldDatabase = worldDatabase;
        _creatureDisplayInfoRecords = creatureDisplayInfoRecords;
        _creatureModelDataRecords = creatureModelDataRecords;
    }

    public CreatureModelInfo GetCreatureModelInfo(uint modelId)
    {
        return _creatureModelStorage.LookupByKey(modelId);
    }

    public CreatureModelInfo GetCreatureModelRandomGender(ref CreatureModel model, CreatureTemplate creatureTemplate)
    {
        var modelInfo = GetCreatureModelInfo(model.CreatureDisplayId);

        if (modelInfo == null)
            return null;

        // If a model for another gender exists, 50% chance to use it
        if (modelInfo.DisplayIdOtherGender != 0 && RandomHelper.URand(0, 1) == 0)
        {
            var minfotmp = GetCreatureModelInfo(modelInfo.DisplayIdOtherGender);

            if (minfotmp == null)
                Log.Logger.Error($"Model (Entry: {model.CreatureDisplayId}) has modelidothergender {modelInfo.DisplayIdOtherGender} not found in table `creaturemodelinfo`. ");
            else
            {
                // DisplayID changed
                model.CreatureDisplayId = modelInfo.DisplayIdOtherGender;

                var creatureModel = creatureTemplate?.Models.Find(templateModel => templateModel.CreatureDisplayId == modelInfo.DisplayIdOtherGender);

                if (creatureModel != null)
                    model = creatureModel;

                return minfotmp;
            }
        }

        return modelInfo;
    }

    public void Load()
    {
        var time = Time.MSTime;
        var result = _worldDatabase.Query("SELECT DisplayID, BoundingRadius, CombatReach, DisplayID_Other_Gender FROM creature_model_info");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature model definitions. DB table `creaturemodelinfo` is empty.");

            return;
        }

        // List of model FileDataIDs that the client treats as invisible stalker
        uint[] trigggerCreatureModelFileID =
        {
            124640, 124641, 124642, 343863, 439302
        };

        uint count = 0;

        do
        {
            var displayId = result.Read<uint>(0);

            if (!_creatureDisplayInfoRecords.TryGetValue(displayId, out var creatureDisplay))
            {
                Log.Logger.Debug("Table `creature_model_info` has a non-existent DisplayID (ID: {0}). Skipped.", displayId);

                continue;
            }

            CreatureModelInfo modelInfo = new()
            {
                BoundingRadius = result.Read<float>(1),
                CombatReach = result.Read<float>(2),
                DisplayIdOtherGender = result.Read<uint>(3),
                Gender = creatureDisplay.Gender
            };

            // Checks
            if (modelInfo.Gender == (sbyte)Gender.Unknown)
                modelInfo.Gender = (sbyte)Gender.Male;

            if (modelInfo.DisplayIdOtherGender != 0 && !_creatureDisplayInfoRecords.ContainsKey(modelInfo.DisplayIdOtherGender))
            {
                Log.Logger.Debug("Table `creature_model_info` has a non-existent DisplayID_Other_Gender (ID: {0}) being used by DisplayID (ID: {1}).", modelInfo.DisplayIdOtherGender, displayId);
                modelInfo.DisplayIdOtherGender = 0;
            }

            if (modelInfo.CombatReach < 0.1f)
                modelInfo.CombatReach = SharedConst.DefaultPlayerCombatReach;

            if (_creatureModelDataRecords.TryGetValue(creatureDisplay.ModelID, out var modelData))
                for (uint i = 0; i < 5; ++i)
                    if (modelData.FileDataID == trigggerCreatureModelFileID[i])
                    {
                        modelInfo.IsTrigger = true;

                        break;
                    }

            _creatureModelStorage.Add(displayId, modelInfo);
            count++;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} creature model based info in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }
}