// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.Entities;
using Serilog;
using Microsoft.Extensions.Configuration;
using Forged.RealmServer.Globals;
using Framework.Util;

namespace Forged.RealmServer.DataStorage;

public class AreaTriggerDataStorage
{
	readonly Dictionary<(uint mapId, uint cellId), SortedSet<ulong>> _areaTriggerSpawnsByLocation = new();
	readonly Dictionary<ulong, AreaTriggerSpawn> _areaTriggerSpawnsBySpawnId = new();
	readonly Dictionary<AreaTriggerId, AreaTriggerTemplate> _areaTriggerTemplateStore = new();
	readonly Dictionary<uint, AreaTriggerCreateProperties> _areaTriggerCreateProperties = new();
    private readonly IConfiguration _configuration;
    private readonly GameObjectManager _gameObjectManager;
    private readonly WorldDatabase _worldDatabase;
    private readonly CliDB _cliDb;

    AreaTriggerDataStorage(IConfiguration configuration, GameObjectManager gameObjectManager, WorldDatabase worldDatabase, CliDB cliDB)
    {
        _configuration = configuration;
        _gameObjectManager = gameObjectManager;
        _worldDatabase = worldDatabase;
        _cliDb = cliDB;
    }

    public void LoadAreaTriggerTemplates()
	{
		var oldMSTime = Time.MSTime;
		MultiMap<uint, Vector2> verticesByCreateProperties = new();
		MultiMap<uint, Vector2> verticesTargetByCreateProperties = new();
		MultiMap<uint, Vector3> splinesByCreateProperties = new();
		MultiMap<AreaTriggerId, AreaTriggerAction> actionsByAreaTrigger = new();

		//                                                       0         1             2            3           4
		var templateActions = _worldDatabase.Query("SELECT AreaTriggerId, IsServerSide, ActionType, ActionParam, TargetType FROM `areatrigger_template_actions`");

		if (!templateActions.IsEmpty())
			do
			{
				AreaTriggerId areaTriggerId = new(templateActions.Read<uint>(0), templateActions.Read<byte>(1) == 1);

				AreaTriggerAction action;
				action.Param = templateActions.Read<uint>(3);
				action.ActionType = (AreaTriggerActionTypes)templateActions.Read<uint>(2);
				action.TargetType = (AreaTriggerActionUserTypes)templateActions.Read<uint>(4);

				if (action.ActionType >= AreaTriggerActionTypes.Max)
				{
					Log.Logger.Error($"Table `areatrigger_template_actions` has invalid ActionType ({action.ActionType}, IsServerSide: {areaTriggerId.IsServerSide}) for AreaTriggerId {areaTriggerId.Id} and Param {action.Param}");

					continue;
				}

				if (action.TargetType >= AreaTriggerActionUserTypes.Max)
				{
					Log.Logger.Error($"Table `areatrigger_template_actions` has invalid TargetType ({action.TargetType}, IsServerSide: {areaTriggerId.IsServerSide}) for AreaTriggerId {areaTriggerId} and Param {action.Param}");

					continue;
				}


				if (action.ActionType == AreaTriggerActionTypes.Teleport)
					if (_gameObjectManager.GetWorldSafeLoc(action.Param) == null)
					{
						Log.Logger.Error($"Table `areatrigger_template_actions` has invalid (Id: {areaTriggerId}, IsServerSide: {areaTriggerId.IsServerSide}) with TargetType=Teleport and Param ({action.Param}) not a valid world safe loc entry");

						continue;
					}

				actionsByAreaTrigger.Add(areaTriggerId, action);
			} while (templateActions.NextRow());
		else
			Log.Logger.Information("Loaded 0 AreaTrigger templates actions. DB table `areatrigger_template_actions` is empty.");

		//                                           0                              1    2         3         4               5
		var vertices = _worldDatabase.Query("SELECT AreaTriggerCreatePropertiesId, Idx, VerticeX, VerticeY, VerticeTargetX, VerticeTargetY FROM `areatrigger_create_properties_polygon_vertex` ORDER BY `AreaTriggerCreatePropertiesId`, `Idx`");

		if (!vertices.IsEmpty())
			do
			{
				var areaTriggerCreatePropertiesId = vertices.Read<uint>(0);

				verticesByCreateProperties.Add(areaTriggerCreatePropertiesId, new Vector2(vertices.Read<float>(2), vertices.Read<float>(3)));

				if (!vertices.IsNull(4) && !vertices.IsNull(5))
					verticesTargetByCreateProperties.Add(areaTriggerCreatePropertiesId, new Vector2(vertices.Read<float>(4), vertices.Read<float>(5)));
				else if (vertices.IsNull(4) != vertices.IsNull(5))
					Log.Logger.Error($"Table `areatrigger_create_properties_polygon_vertex` has listed invalid target vertices (AreaTriggerCreatePropertiesId: {areaTriggerCreatePropertiesId}, Index: {vertices.Read<uint>(1)}).");
			} while (vertices.NextRow());
		else
			Log.Logger.Information("Loaded 0 AreaTrigger polygon polygon vertices. DB table `areatrigger_create_properties_polygon_vertex` is empty.");

		//                                         0                              1  2  3
		var splines = _worldDatabase.Query("SELECT AreaTriggerCreatePropertiesId, X, Y, Z FROM `areatrigger_create_properties_spline_point` ORDER BY `AreaTriggerCreatePropertiesId`, `Idx`");

		if (!splines.IsEmpty())
			do
			{
				var areaTriggerCreatePropertiesId = splines.Read<uint>(0);
				Vector3 spline = new(splines.Read<float>(1), splines.Read<float>(2), splines.Read<float>(3));

				splinesByCreateProperties.Add(areaTriggerCreatePropertiesId, spline);
			} while (splines.NextRow());
		else
			Log.Logger.Information("Loaded 0 AreaTrigger splines. DB table `areatrigger_create_properties_spline_point` is empty.");

		//                                            0   1             2
		var templates = _worldDatabase.Query("SELECT Id, IsServerSide, Flags FROM `areatrigger_template`");

		if (!templates.IsEmpty())
			do
			{
				AreaTriggerTemplate areaTriggerTemplate = new();
				areaTriggerTemplate.Id = new AreaTriggerId(templates.Read<uint>(0), templates.Read<byte>(1) == 1);

				areaTriggerTemplate.Flags = (AreaTriggerFlags)templates.Read<uint>(2);

				if (areaTriggerTemplate.Id.IsServerSide && areaTriggerTemplate.Flags != 0)
				{
					if (_configuration.GetDefaultValue("load.autoclean", false))
						_worldDatabase.Execute($"DELETE FROM areatrigger_template WHERE Id = {areaTriggerTemplate.Id}");
					else
						Log.Logger.Error($"Table `areatrigger_template` has listed server-side areatrigger (Id: {areaTriggerTemplate.Id.Id}, IsServerSide: {areaTriggerTemplate.Id.IsServerSide}) with none-zero flags");

					continue;
				}

				areaTriggerTemplate.Actions = actionsByAreaTrigger[areaTriggerTemplate.Id];

				_areaTriggerTemplateStore[areaTriggerTemplate.Id] = areaTriggerTemplate;
			} while (templates.NextRow());

		//                                                              0   1              2            3             4             5              6       7          8                  9             10
		var areatriggerCreateProperties = _worldDatabase.Query("SELECT Id, AreaTriggerId, MoveCurveId, ScaleCurveId, MorphCurveId, FacingCurveId, AnimId, AnimKitId, DecalPropertiesId, TimeToTarget, TimeToTargetScale, " +
														//11     12          13          14          15          16          17          18          19          20
														"Shape, ShapeData0, ShapeData1, ShapeData2, ShapeData3, ShapeData4, ShapeData5, ShapeData6, ShapeData7, ScriptName FROM `areatrigger_create_properties`");

		if (!areatriggerCreateProperties.IsEmpty())
			do
			{
				AreaTriggerCreateProperties createProperties = new();
				createProperties.Id = areatriggerCreateProperties.Read<uint>(0);

				var areatriggerId = areatriggerCreateProperties.Read<uint>(1);
				createProperties.Template = GetAreaTriggerTemplate(new AreaTriggerId(areatriggerId, false));

				var shape = (AreaTriggerTypes)areatriggerCreateProperties.Read<byte>(11);

				if (areatriggerId != 0 && createProperties.Template == null)
				{
					Log.Logger.Error($"Table `areatrigger_create_properties` reference invalid AreaTriggerId {areatriggerId} for AreaTriggerCreatePropertiesId {createProperties.Id}");

					continue;
				}

				if (shape >= AreaTriggerTypes.Max)
				{
					Log.Logger.Error($"Table `areatrigger_create_properties` has listed areatrigger create properties {createProperties.Id} with invalid shape {shape}.");

					continue;
				}

				uint ValidateAndSetCurve(uint value)
				{
					if (value != 0 && !_cliDb.CurveStorage.ContainsKey(value))
					{
						Log.Logger.Error($"Table `areatrigger_create_properties` has listed areatrigger (AreaTriggerCreatePropertiesId: {createProperties.Id}, Id: {areatriggerId}) with invalid Curve ({value}), set to 0!");

						return 0;
					}

					return value;
				}

				createProperties.MoveCurveId = ValidateAndSetCurve(areatriggerCreateProperties.Read<uint>(2));
				createProperties.ScaleCurveId = ValidateAndSetCurve(areatriggerCreateProperties.Read<uint>(3));
				createProperties.MorphCurveId = ValidateAndSetCurve(areatriggerCreateProperties.Read<uint>(4));
				createProperties.FacingCurveId = ValidateAndSetCurve(areatriggerCreateProperties.Read<uint>(5));

				createProperties.AnimId = areatriggerCreateProperties.Read<int>(6);
				createProperties.AnimKitId = areatriggerCreateProperties.Read<uint>(7);
				createProperties.DecalPropertiesId = areatriggerCreateProperties.Read<uint>(8);

				createProperties.TimeToTarget = areatriggerCreateProperties.Read<uint>(9);
				createProperties.TimeToTargetScale = areatriggerCreateProperties.Read<uint>(10);

				createProperties.Shape.TriggerType = shape;

				unsafe
				{
					for (byte i = 0; i < SharedConst.MaxAreatriggerEntityData; ++i)
						createProperties.Shape.DefaultDatas.Data[i] = areatriggerCreateProperties.Read<float>(12 + i);
				}

				createProperties.ScriptIds.Add(_gameObjectManager.GetScriptId(areatriggerCreateProperties.Read<string>(20)));

				if (shape == AreaTriggerTypes.Polygon)
					if (createProperties.Shape.PolygonDatas.Height <= 0.0f)
						createProperties.Shape.PolygonDatas.Height = 1.0f;

				createProperties.PolygonVertices = verticesByCreateProperties[createProperties.Id];
				createProperties.PolygonVerticesTarget = verticesTargetByCreateProperties[createProperties.Id];
				createProperties.SplinePoints = splinesByCreateProperties[createProperties.Id];

				_areaTriggerCreateProperties[createProperties.Id] = createProperties;
			} while (areatriggerCreateProperties.NextRow());
		else
			Log.Logger.Information("Loaded 0 AreaTrigger create properties. DB table `areatrigger_create_properties` is empty.");

		//                                                       0                               1           2             3                4             5        6                 7
		var circularMovementInfos = _worldDatabase.Query("SELECT AreaTriggerCreatePropertiesId, StartDelay, CircleRadius, BlendFromRadius, InitialAngle, ZOffset, CounterClockwise, CanLoop FROM `areatrigger_create_properties_orbit`");

		if (!circularMovementInfos.IsEmpty())
			do
			{
				var areaTriggerCreatePropertiesId = circularMovementInfos.Read<uint>(0);

				var createProperties = _areaTriggerCreateProperties.LookupByKey(areaTriggerCreatePropertiesId);

				if (createProperties == null)
				{
					Log.Logger.Error($"Table `areatrigger_create_properties_orbit` reference invalid AreaTriggerCreatePropertiesId {areaTriggerCreatePropertiesId}");

					continue;
				}

				AreaTriggerOrbitInfo orbitInfo = new();

				orbitInfo.StartDelay = circularMovementInfos.Read<uint>(1);
				orbitInfo.Radius = circularMovementInfos.Read<float>(2);

				if (!float.IsFinite(orbitInfo.Radius))
				{
					Log.Logger.Error($"Table `areatrigger_create_properties_orbit` has listed areatrigger (AreaTriggerCreatePropertiesId: {areaTriggerCreatePropertiesId}) with invalid Radius ({orbitInfo.Radius}), set to 0!");
					orbitInfo.Radius = 0.0f;
				}

				orbitInfo.BlendFromRadius = circularMovementInfos.Read<float>(3);

				if (!float.IsFinite(orbitInfo.BlendFromRadius))
				{
					Log.Logger.Error($"Table `areatrigger_create_properties_orbit` has listed areatrigger (AreaTriggerCreatePropertiesId: {areaTriggerCreatePropertiesId}) with invalid BlendFromRadius ({orbitInfo.BlendFromRadius}), set to 0!");
					orbitInfo.BlendFromRadius = 0.0f;
				}

				orbitInfo.InitialAngle = circularMovementInfos.Read<float>(4);

				if (!float.IsFinite(orbitInfo.InitialAngle))
				{
					Log.Logger.Error($"Table `areatrigger_create_properties_orbit` has listed areatrigger (AreaTriggerCreatePropertiesId: {areaTriggerCreatePropertiesId}) with invalid InitialAngle ({orbitInfo.InitialAngle}), set to 0!");
					orbitInfo.InitialAngle = 0.0f;
				}

				orbitInfo.ZOffset = circularMovementInfos.Read<float>(5);

				if (!float.IsFinite(orbitInfo.ZOffset))
				{
					Log.Logger.Error($"Table `spell_areatrigger_circular` has listed areatrigger (MiscId: {areaTriggerCreatePropertiesId}) with invalid ZOffset ({orbitInfo.ZOffset}), set to 0!");
					orbitInfo.ZOffset = 0.0f;
				}

				orbitInfo.CounterClockwise = circularMovementInfos.Read<bool>(6);
				orbitInfo.CanLoop = circularMovementInfos.Read<bool>(7);

				createProperties.OrbitInfo = orbitInfo;
			} while (circularMovementInfos.NextRow());
		else
			Log.Logger.Information("Loaded 0 AreaTrigger templates circular movement infos. DB table `areatrigger_create_properties_orbit` is empty.");

		Log.Logger.Information($"Loaded {_areaTriggerTemplateStore.Count} spell areatrigger templates in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
	}

	public AreaTriggerTemplate GetAreaTriggerTemplate(AreaTriggerId areaTriggerId)
	{
		return _areaTriggerTemplateStore.LookupByKey(areaTriggerId);
	}

	public AreaTriggerCreateProperties GetAreaTriggerCreateProperties(uint spellMiscValue)
	{
		if (!_areaTriggerCreateProperties.TryGetValue(spellMiscValue, out var val))
		{
			Log.Logger.Warning($"AreaTriggerCreateProperties did not exist for {spellMiscValue}. Using default area trigger properties.");
			val = AreaTriggerCreateProperties.CreateDefault(spellMiscValue);
			_areaTriggerCreateProperties[spellMiscValue] = val;
		}

		return val;
	}

	public SortedSet<ulong> GetAreaTriggersForMapAndCell(uint mapId, uint cellId)
	{
		return _areaTriggerSpawnsByLocation.LookupByKey((mapId, cellId));
	}

	public AreaTriggerSpawn GetAreaTriggerSpawn(ulong spawnId)
	{
		return _areaTriggerSpawnsBySpawnId.LookupByKey(spawnId);
	}
}