// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.DataStorage;

public class AreaTriggerDataStorage
{
    private readonly Dictionary<uint, AreaTriggerCreateProperties> _areaTriggerCreateProperties = new();
    private readonly Dictionary<(uint mapId, uint cellId), SortedSet<ulong>> _areaTriggerSpawnsByLocation = new();
    private readonly Dictionary<ulong, AreaTriggerSpawn> _areaTriggerSpawnsBySpawnId = new();
    private readonly Dictionary<AreaTriggerId, AreaTriggerTemplate> _areaTriggerTemplateStore = new();
    private readonly CliDB _cliDB;
    private readonly GridDefines _gridDefines;
    private readonly ScriptManager _scriptManager;
    private readonly WorldSafeLocationsCache _worldSafeLocationsCache;
    private readonly IConfiguration _configuration;
    private readonly GameObjectManager _objectManager;
    private readonly WorldDatabase _worldDatabase;

    public AreaTriggerDataStorage(WorldDatabase worldDatabase, GameObjectManager objectManager, IConfiguration configuration, CliDB cliDB, 
                                  GridDefines gridDefines, ScriptManager scriptManager, WorldSafeLocationsCache worldSafeLocationsCache)
    {
        _worldDatabase = worldDatabase;
        _objectManager = objectManager;
        _configuration = configuration;
        _cliDB = cliDB;
        _gridDefines = gridDefines;
        _scriptManager = scriptManager;
        _worldSafeLocationsCache = worldSafeLocationsCache;
    }

    public AreaTriggerCreateProperties GetAreaTriggerCreateProperties(uint spellMiscValue)
    {
        if (_areaTriggerCreateProperties.TryGetValue(spellMiscValue, out var val))
            return val;

        Log.Logger.Warning($"AreaTriggerCreateProperties did not exist for {spellMiscValue}. Using default area trigger properties.");
        val = AreaTriggerCreateProperties.CreateDefault(spellMiscValue, _scriptManager);
        _areaTriggerCreateProperties[spellMiscValue] = val;

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

    public AreaTriggerTemplate GetAreaTriggerTemplate(AreaTriggerId areaTriggerId)
    {
        return _areaTriggerTemplateStore.LookupByKey(areaTriggerId);
    }

    public void LoadAreaTriggerSpawns()
    {
        var oldMSTime = Time.MSTime;

        // Load area trigger positions (to put them on the server)
        //                                            0        1              2             3      4     5     6     7            8              9        10
        var templates = _worldDatabase.Query("SELECT SpawnId, AreaTriggerId, IsServerSide, MapId, PosX, PosY, PosZ, Orientation, PhaseUseFlags, PhaseId, PhaseGroup, " +
                                             //11     12          13          14          15          16          17          18          19          20
                                             "Shape, ShapeData0, ShapeData1, ShapeData2, ShapeData3, ShapeData4, ShapeData5, ShapeData6, ShapeData7, ScriptName FROM `areatrigger`");

        if (!templates.IsEmpty())
            do
            {
                var spawnId = templates.Read<ulong>(0);
                AreaTriggerId areaTriggerId = new(templates.Read<uint>(1), templates.Read<byte>(2) == 1);
                WorldLocation location = new(templates.Read<uint>(3), templates.Read<float>(4), templates.Read<float>(5), templates.Read<float>(6), templates.Read<float>(7));
                var shape = (AreaTriggerTypes)templates.Read<byte>(11);

                if (GetAreaTriggerTemplate(areaTriggerId) == null)
                {
                    Log.Logger.Error($"Table `areatrigger` has listed areatrigger that doesn't exist: Id: {areaTriggerId.Id}, IsServerSide: {areaTriggerId.IsServerSide} for SpawnId {spawnId}");

                    continue;
                }

                if (!_gridDefines.IsValidMapCoord(location))
                {
                    Log.Logger.Error($"Table `areatrigger` has listed an invalid position: SpawnId: {spawnId}, MapId: {location.MapId}, Position: {location}");

                    continue;
                }

                if (shape >= AreaTriggerTypes.Max)
                {
                    Log.Logger.Error($"Table `areatrigger` has listed areatrigger SpawnId: {spawnId} with invalid shape {shape}.");

                    continue;
                }

                AreaTriggerSpawn spawn = new()
                {
                    SpawnId = spawnId,
                    MapId = location.MapId,
                    TriggerId = areaTriggerId,
                    SpawnPoint = new Position(location),
                    PhaseUseFlags = (PhaseUseFlagsValues)templates.Read<byte>(8),
                    PhaseId = templates.Read<uint>(9),
                    PhaseGroup = templates.Read<uint>(10),
                    Shape =
                    {
                        TriggerType = shape
                    }
                };

                unsafe
                {
                    for (var i = 0; i < SharedConst.MaxAreatriggerEntityData; ++i)
                        spawn.Shape.DefaultDatas.Data[i] = templates.Read<float>(12 + i);
                }

                spawn.ScriptId = _scriptManager.GetScriptId(templates.Read<string>(20));
                spawn.SpawnGroupData = _objectManager.SpawnGroupDataCache.GetLegacySpawnGroup();

                // Add the trigger to a map::cell map, which is later used by GridLoader to query
                var cellCoord = _gridDefines.ComputeCellCoord(spawn.SpawnPoint.X, spawn.SpawnPoint.Y);

                if (!_areaTriggerSpawnsByLocation.TryGetValue((spawn.MapId, cellCoord.GetId()), out var val))
                {
                    val = new SortedSet<ulong>();
                    _areaTriggerSpawnsByLocation[(spawn.MapId, cellCoord.GetId())] = val;
                }

                val.Add(spawnId);

                // add the position to the map
                _areaTriggerSpawnsBySpawnId[spawnId] = spawn;
            } while (templates.NextRow());

        Log.Logger.Information($"Loaded {_areaTriggerSpawnsBySpawnId.Count} areatrigger spawns in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
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
                    if (_worldSafeLocationsCache.GetWorldSafeLoc(action.Param) == null)
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
                AreaTriggerTemplate areaTriggerTemplate = new()
                {
                    Id = new AreaTriggerId(templates.Read<uint>(0), templates.Read<byte>(1) == 1),
                    Flags = (AreaTriggerFlags)templates.Read<uint>(2)
                };

                if (areaTriggerTemplate.Id.IsServerSide && areaTriggerTemplate.Flags != 0)
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
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
                AreaTriggerCreateProperties createProperties = new()
                {
                    Id = areatriggerCreateProperties.Read<uint>(0)
                };

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
                    if (value != 0 && !_cliDB.CurveStorage.ContainsKey(value))
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

                createProperties.ScriptIds.Add(_scriptManager.GetScriptId(areatriggerCreateProperties.Read<string>(20)));

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

                if (!_areaTriggerCreateProperties.TryGetValue(areaTriggerCreatePropertiesId, out var createProperties))
                {
                    Log.Logger.Error($"Table `areatrigger_create_properties_orbit` reference invalid AreaTriggerCreatePropertiesId {areaTriggerCreatePropertiesId}");

                    continue;
                }

                AreaTriggerOrbitInfo orbitInfo = new()
                {
                    StartDelay = circularMovementInfos.Read<uint>(1),
                    Radius = circularMovementInfos.Read<float>(2)
                };

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
}