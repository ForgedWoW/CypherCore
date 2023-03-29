// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Achievements;
using Forged.RealmServer.DataStorage;
using Framework.Constants;
using Framework.Database;
using System;
using System.Collections.Generic;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.RealmServer.Scenarios;

public class ScenarioManager
{
    private readonly IConfiguration _configuration;
    private readonly WorldDatabase _worldDatabase;
    private readonly CliDB _cliDb;
    private readonly CriteriaManager _criteriaManager;
    readonly Dictionary<uint, ScenarioData> _scenarioData = new();
	readonly MultiMap<uint, ScenarioPOI> _scenarioPOIStore = new();
	readonly Dictionary<Tuple<uint, byte>, ScenarioDBData> _scenarioDBData = new();

	public ScenarioManager(IConfiguration configuration, WorldDatabase worldDatabase, CliDB cliDB, CriteriaManager criteriaManager)
    {
        _configuration = configuration;
        _worldDatabase = worldDatabase;
        _cliDb = cliDB;
        _criteriaManager = criteriaManager;
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

			if (scenarioHordeId > 0 && !_scenarioData.ContainsKey(scenarioHordeId))
			{
				Log.Logger.Error("ScenarioMgr.LoadDBData: DB Table `scenarios`, column scenario_H contained an invalid scenario (Id: {0})!", scenarioHordeId);

				continue;
			}

			if (scenarioHordeId == 0)
				scenarioHordeId = scenarioAllianceId;

			ScenarioDBData data = new();
			data.MapID = mapId;
			data.DifficultyID = difficulty;
			data.Scenario_A = scenarioAllianceId;
			data.Scenario_H = scenarioHordeId;
			_scenarioDBData[Tuple.Create(mapId, difficulty)] = data;
		} while (result.NextRow());

		Log.Logger.Information("Loaded {0} instance scenario entries in {1} ms", _scenarioDBData.Count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void LoadDB2Data()
	{
		_scenarioData.Clear();

		Dictionary<uint, Dictionary<byte, ScenarioStepRecord>> scenarioSteps = new();
		uint deepestCriteriaTreeSize = 0;

		foreach (var step in _cliDb.ScenarioStepStorage.Values)
		{
			if (!scenarioSteps.ContainsKey(step.ScenarioID))
				scenarioSteps[step.ScenarioID] = new Dictionary<byte, ScenarioStepRecord>();

			scenarioSteps[step.ScenarioID][step.OrderIndex] = step;
			var tree = _criteriaManager.GetCriteriaTree(step.CriteriaTreeId);

			if (tree != null)
			{
				uint criteriaTreeSize = 0;
				CriteriaManager.WalkCriteriaTree(tree, treeFunc => { ++criteriaTreeSize; });
				deepestCriteriaTreeSize = Math.Max(deepestCriteriaTreeSize, criteriaTreeSize);
			}
		}

		//ASSERT(deepestCriteriaTreeSize < MAX_ALLOWED_SCENARIO_POI_QUERY_SIZE, "MAX_ALLOWED_SCENARIO_POI_QUERY_SIZE must be at least {0}", deepestCriteriaTreeSize + 1);

		foreach (var scenario in _cliDb.ScenarioStorage.Values)
		{
			ScenarioData data = new();
			data.Entry = scenario;
			data.Steps = scenarioSteps.LookupByKey(scenario.Id);
			_scenarioData[scenario.Id] = data;
		}
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
				var CriteriaTreeID = pointsResult.Read<uint>(0);
				var Idx1 = pointsResult.Read<int>(1);
				var X = pointsResult.Read<int>(2);
				var Y = pointsResult.Read<int>(3);
				var Z = pointsResult.Read<int>(4);

				if (!allPoints.ContainsKey(CriteriaTreeID))
					allPoints[CriteriaTreeID] = new MultiMap<int, ScenarioPOIPoint>();

				allPoints[CriteriaTreeID].Add(Idx1, new ScenarioPOIPoint(X, Y, Z));
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

			var blobs = allPoints.LookupByKey(criteriaTreeID);

			if (blobs != null)
			{
				var points = blobs.LookupByKey(idx1);

				if (!points.Empty())
				{
					_scenarioPOIStore.Add(criteriaTreeID, new ScenarioPOI(blobIndex, mapID, uiMapID, priority, flags, worldEffectID, playerConditionID, navigationPlayerConditionID, points));
					++count;

					continue;
				}
			}

			if (_configuration.GetDefaultValue("load.autoclean", false))
				_worldDatabase.Execute($"DELETE FROM scenario_poi WHERE criteriaTreeID = {criteriaTreeID}");
			else
				Log.Logger.Error($"Table scenario_poi references unknown scenario poi points for criteria tree id {criteriaTreeID} POI id {blobIndex}");
		} while (result.NextRow());

		Log.Logger.Information($"Loaded {count} scenario POI definitions in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
	}

	public List<ScenarioPOI> GetScenarioPOIs(uint CriteriaTreeID)
	{
		if (!_scenarioPOIStore.ContainsKey(CriteriaTreeID))
			return null;

		return _scenarioPOIStore[CriteriaTreeID];
	}
}