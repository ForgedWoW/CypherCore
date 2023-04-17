// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Achievements;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Maps;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Scenarios;

public class ScenarioManager
{
    private readonly CliDB _cliDB;
    private readonly ClassFactory _classFactory;
    private readonly IConfiguration _configuration;
    private readonly CriteriaManager _criteriaManager;
    private readonly Dictionary<uint, ScenarioData> _scenarioData = new();
    private readonly Dictionary<Tuple<uint, byte>, ScenarioDBData> _scenarioDBData = new();
    private readonly MultiMap<uint, ScenarioPOI> _scenarioPOIStore = new();
    private readonly WorldDatabase _worldDatabase;

    public ScenarioManager(WorldDatabase worldDatabase, IConfiguration configuration, CriteriaManager criteriaManager, CliDB cliDB, ClassFactory classFactory)
    {
        _worldDatabase = worldDatabase;
        _configuration = configuration;
        _criteriaManager = criteriaManager;
        _cliDB = cliDB;
        _classFactory = classFactory;
    }

    public InstanceScenario CreateInstanceScenario(InstanceMap map, int team)
    {
        var dbData = _scenarioDBData.LookupByKey(Tuple.Create(map.Id, (byte)map.DifficultyID));

        // No scenario registered for this map and difficulty in the database
        if (dbData == null)
            return null;

        uint scenarioID = team switch
        {
            TeamIds.Alliance => dbData.ScenarioA,
            TeamIds.Horde    => dbData.ScenarioH,
            _                => 0
        };

        if (_scenarioData.TryGetValue(scenarioID, out var scenarioData))
            return _classFactory.ResolvePositional<InstanceScenario>(map, scenarioData);

        Log.Logger.Error("Table `scenarios` contained data linking scenario (Id: {0}) to map (Id: {1}), difficulty (Id: {2}) but no scenario data was found related to that scenario Id.",
                         scenarioID,
                         map.Id,
                         map.DifficultyID);

        return null;
    }

    public List<ScenarioPOI> GetScenarioPoIs(uint criteriaTreeID)
    {
        return !_scenarioPOIStore.ContainsKey(criteriaTreeID) ? null : _scenarioPOIStore[criteriaTreeID];
    }

    public void LoadDB2Data()
    {
        _scenarioData.Clear();

        Dictionary<uint, Dictionary<byte, ScenarioStepRecord>> scenarioSteps = new();
        uint deepestCriteriaTreeSize = 0;

        foreach (var step in _cliDB.ScenarioStepStorage.Values)
        {
            if (!scenarioSteps.ContainsKey(step.ScenarioID))
                scenarioSteps[step.ScenarioID] = new Dictionary<byte, ScenarioStepRecord>();

            scenarioSteps[step.ScenarioID][step.OrderIndex] = step;
            var tree = _criteriaManager.GetCriteriaTree(step.CriteriaTreeId);

            if (tree == null)
                continue;

            uint criteriaTreeSize = 0;
            CriteriaManager.WalkCriteriaTree(tree, _ => { ++criteriaTreeSize; });
            deepestCriteriaTreeSize = Math.Max(deepestCriteriaTreeSize, criteriaTreeSize);
        }

        foreach (var scenario in _cliDB.ScenarioStorage.Values)
        {
            ScenarioData data = new()
            {
                Entry = scenario,
                Steps = scenarioSteps.LookupByKey(scenario.Id)
            };

            _scenarioData[scenario.Id] = data;
        }
    }

    public void LoadDBData()
    {
        _scenarioDBData.Clear();

        var oldMSTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT map, difficulty, scenario_A, scenario_H FROM scenarios");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 scenarios. DB table `scenarios` is empty!");

            return;
        }

        do
        {
            var mapId = result.Read<uint>(0);
            var difficulty = result.Read<byte>(1);

            var scenarioAllianceId = result.Read<uint>(2);

            if (scenarioAllianceId > 0 && !_scenarioData.ContainsKey(scenarioAllianceId))
            {
                Log.Logger.Error("ScenarioMgr.LoadDBData: DB Table `scenarios`, column scenario_A contained an invalid scenario (Id: {0})!", scenarioAllianceId);

                continue;
            }

            var scenarioHordeId = result.Read<uint>(3);

            switch (scenarioHordeId)
            {
                case > 0 when !_scenarioData.ContainsKey(scenarioHordeId):
                    Log.Logger.Error("ScenarioMgr.LoadDBData: DB Table `scenarios`, column scenario_H contained an invalid scenario (Id: {0})!", scenarioHordeId);

                    continue;
                case 0:
                    scenarioHordeId = scenarioAllianceId;

                    break;
            }

            ScenarioDBData data = new()
            {
                MapID = mapId,
                DifficultyID = difficulty,
                ScenarioA = scenarioAllianceId,
                ScenarioH = scenarioHordeId
            };

            _scenarioDBData[Tuple.Create(mapId, difficulty)] = data;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} instance scenario entries in {1} ms", _scenarioDBData.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadScenarioPOI()
    {
        var oldMSTime = Time.MSTime;

        _scenarioPOIStore.Clear(); // need for reload case

        uint count = 0;

        //                                         0               1          2     3      4        5         6      7              8                  9
        var result = _worldDatabase.Query("SELECT CriteriaTreeID, BlobIndex, Idx1, MapID, UiMapID, Priority, Flags, WorldEffectID, PlayerConditionID, NavigationPlayerConditionID FROM scenario_poi ORDER BY CriteriaTreeID, Idx1");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 scenario POI definitions. DB table `scenario_poi` is empty.");

            return;
        }

        Dictionary<uint, MultiMap<int, ScenarioPOIPoint>> allPoints = new();

        //                                               0               1    2  3  4
        var pointsResult = _worldDatabase.Query("SELECT CriteriaTreeID, Idx1, X, Y, Z FROM scenario_poi_points ORDER BY CriteriaTreeID DESC, Idx1, Idx2");

        if (!pointsResult.IsEmpty())
            do
            {
                var criteriaTreeID = pointsResult.Read<uint>(0);
                var idx1 = pointsResult.Read<int>(1);
                var x = pointsResult.Read<int>(2);
                var y = pointsResult.Read<int>(3);
                var z = pointsResult.Read<int>(4);

                if (!allPoints.ContainsKey(criteriaTreeID))
                    allPoints[criteriaTreeID] = new MultiMap<int, ScenarioPOIPoint>();

                allPoints[criteriaTreeID].Add(idx1, new ScenarioPOIPoint(x, y, z));
            } while (pointsResult.NextRow());

        do
        {
            var criteriaTreeID = result.Read<uint>(0);
            var blobIndex = result.Read<int>(1);
            var idx1 = result.Read<int>(2);
            var mapID = result.Read<int>(3);
            var uiMapID = result.Read<int>(4);
            var priority = result.Read<int>(5);
            var flags = result.Read<int>(6);
            var worldEffectID = result.Read<int>(7);
            var playerConditionID = result.Read<int>(8);
            var navigationPlayerConditionID = result.Read<int>(9);

            if (_criteriaManager.GetCriteriaTree(criteriaTreeID) == null)
                Log.Logger.Error($"`scenario_poi` CriteriaTreeID ({criteriaTreeID}) Idx1 ({idx1}) does not correspond to a valid criteria tree");

            if (allPoints.TryGetValue(criteriaTreeID, out var blobs))
                if (blobs.TryGetValue(idx1, out var points))
                {
                    _scenarioPOIStore.Add(criteriaTreeID, new ScenarioPOI(blobIndex, mapID, uiMapID, priority, flags, worldEffectID, playerConditionID, navigationPlayerConditionID, points));
                    ++count;

                    continue;
                }

            if (_configuration.GetDefaultValue("load:autoclean", false))
                _worldDatabase.Execute($"DELETE FROM scenario_poi WHERE criteriaTreeID = {criteriaTreeID}");
            else
                Log.Logger.Error($"Table scenario_poi references unknown scenario poi points for criteria tree id {criteriaTreeID} POI id {blobIndex}");
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} scenario POI definitions in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }
}