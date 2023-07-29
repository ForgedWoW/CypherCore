using System;
using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.G;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.DataStorage.Structs.P;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Phasing;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class GameObjectCache : IObjectCache
{
    private readonly WorldDatabase _worldDatabase;
    private readonly DB2Manager _db2Manager;
    private readonly SpawnGroupDataCache _spawnGroupDataCache;
    private readonly ScriptManager _scriptManager;
    private readonly DB6Storage<GameObjectDisplayInfoRecord> _gameObjectDisplayInfoRecords;
    private readonly DB6Storage<MapRecord> _mapRecords;
    private readonly DB6Storage<PhaseRecord> _phaseRecords;
    private readonly GridDefines _gridDefines;
    private readonly IConfiguration _configuration;
    private readonly PhasingHandler _phasingHandler;
    private readonly TerrainManager _terrainManager;
    private readonly GameObjectTemplateCache _gameObjectTemplateCache;
    private readonly MapObjectCache _mapObjectCache;
    private readonly MapSpawnGroupCache _mapSpawnGroupCache;
    private readonly CreatureDataCache _creatureDataCache;

    public GameObjectCache(WorldDatabase worldDatabase, DB2Manager db2Manager, SpawnGroupDataCache spawnGroupDataCache, ScriptManager scriptManager,
                           DB6Storage<GameObjectDisplayInfoRecord> gameObjectDisplayInfoRecords, DB6Storage<MapRecord> mapRecords, DB6Storage<PhaseRecord> phaseRecords,
                           GridDefines gridDefines, IConfiguration configuration, PhasingHandler phasingHandler, TerrainManager terrainManager,
                           GameObjectTemplateCache gameObjectTemplateCache, MapObjectCache mapObjectCache, MapSpawnGroupCache mapSpawnGroupCache,
                           CreatureDataCache creatureDataCache)
    {
        _worldDatabase = worldDatabase;
        _db2Manager = db2Manager;
        _spawnGroupDataCache = spawnGroupDataCache;
        _scriptManager = scriptManager;
        _gameObjectDisplayInfoRecords = gameObjectDisplayInfoRecords;
        _mapRecords = mapRecords;
        _phaseRecords = phaseRecords;
        _gridDefines = gridDefines;
        _configuration = configuration;
        _phasingHandler = phasingHandler;
        _terrainManager = terrainManager;
        _gameObjectTemplateCache = gameObjectTemplateCache;
        _mapObjectCache = mapObjectCache;
        _mapSpawnGroupCache = mapSpawnGroupCache;
        _creatureDataCache = creatureDataCache;
    }

    public Dictionary<ulong, GameObjectData> AllGameObjectData { get; } = new();

    public void DeleteGameObjectData(ulong spawnId)
    {
        var data = GetGameObjectData(spawnId);

        if (data != null)
        {
            _mapObjectCache.RemoveSpawnDataFromGrid(data);
            _mapSpawnGroupCache.OnDeleteSpawnData(data);
        }

        AllGameObjectData.Remove(spawnId);
    }

    public GameObjectData GetGameObjectData(ulong spawnId)
    {
        return AllGameObjectData.LookupByKey(spawnId);
    }

    public void Load()
    {
        var time = Time.MSTime;

        //                                         0                1   2    3           4           5           6
        var result = _worldDatabase.Query("SELECT gameobject.guid, id, map, position_x, position_y, position_z, orientation, " +
                                          //7          8          9          10         11             12            13     14                 15          16
                                          "rotation0, rotation1, rotation2, rotation3, spawntimesecs, animprogress, state, spawnDifficulties, eventEntry, poolSpawnId, " +
                                          //17             18       19          20              21
                                          "phaseUseFlags, phaseid, phasegroup, terrainSwapMap, ScriptName " +
                                          "FROM gameobject LEFT OUTER JOIN game_event_gameobject ON gameobject.guid = game_event_gameobject.guid " +
                                          "LEFT OUTER JOIN pool_members ON pool_members.type = 1 AND gameobject.guid = pool_members.spawnId");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 gameobjects. DB table `gameobject` is empty.");

            return;
        }

        uint count = 0;

        // build single time for check spawnmask
        Dictionary<uint, List<Difficulty>> spawnMasks = new();

        foreach (var mapDifficultyPair in _db2Manager.GetMapDifficulties())
        {
            foreach (var difficultyPair in mapDifficultyPair.Value)
            {
                if (!spawnMasks.ContainsKey(mapDifficultyPair.Key))
                    spawnMasks[mapDifficultyPair.Key] = new List<Difficulty>();

                spawnMasks[mapDifficultyPair.Key].Add((Difficulty)difficultyPair.Key);
            }
        }

        PhaseShift phaseShift = new();

        do
        {
            var guid = result.Read<ulong>(0);
            var entry = result.Read<uint>(1);

            var gInfo = _gameObjectTemplateCache.GetGameObjectTemplate(entry);

            if (gInfo == null)
            {
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0}) with non existing gameobject entry {1}, skipped.", guid, entry);

                continue;
            }

            if (gInfo.displayId == 0)
                switch (gInfo.type)
                {
                    case GameObjectTypes.Trap:
                    case GameObjectTypes.SpellFocus:
                        break;

                    default:
                        Log.Logger.Error("Gameobject (GUID: {0} Entry {1} GoType: {2}) doesn't have a displayId ({3}), not loaded.", guid, entry, gInfo.type, gInfo.displayId);

                        break;
                }

            if (gInfo.displayId != 0 && !_gameObjectDisplayInfoRecords.ContainsKey(gInfo.displayId))
            {
                Log.Logger.Error("Gameobject (GUID: {0} Entry {1} GoType: {2}) has an invalid displayId ({3}), not loaded.", guid, entry, gInfo.type, gInfo.displayId);

                continue;
            }

            GameObjectData data = new()
            {
                SpawnId = guid,
                Id = entry,
                MapId = result.Read<ushort>(2),
                SpawnPoint = new Position(result.Read<float>(3), result.Read<float>(4), result.Read<float>(5), result.Read<float>(6)),
                Rotation = new Quaternion
                {
                    X = result.Read<float>(7),
                    Y = result.Read<float>(8),
                    Z = result.Read<float>(9),
                    W = result.Read<float>(10)
                },
                Spawntimesecs = result.Read<int>(11)
            };

            data.SpawnGroupData = IsTransportMap(data.MapId) ? _spawnGroupDataCache.GetLegacySpawnGroup() : _spawnGroupDataCache.GetDefaultSpawnGroup(); // transport spawns default to compatibility group

            if (!_mapRecords.TryGetValue(data.MapId, out _))
            {
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) spawned on a non-existed map (Id: {2}), skip", guid, data.Id, data.MapId);

                continue;
            }

            if (data.Spawntimesecs == 0 && gInfo.IsDespawnAtAction())
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) with `spawntimesecs` (0) value, but the gameobejct is marked as despawnable at action.", guid, data.Id);

            data.Animprogress = result.Read<uint>(12);
            data.ArtKit = 0;

            var gostate = result.Read<uint>(13);

            if (gostate >= (uint)GameObjectState.Max)
                if (gInfo.type != GameObjectTypes.Transport || gostate > (int)GameObjectState.TransportActive + SharedConst.MaxTransportStopFrames)
                {
                    Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) with invalid `state` ({2}) value, skip", guid, data.Id, gostate);

                    continue;
                }

            data.GoState = (GameObjectState)gostate;

            data.SpawnDifficulties = _creatureDataCache.ParseSpawnDifficulties(result.Read<string>(14), "gameobject", guid, data.MapId, spawnMasks.LookupByKey(data.MapId));

            if (data.SpawnDifficulties.Empty())
            {
                Log.Logger.Error($"Table `creature` has creature (GUID: {guid}) that is not spawned in any difficulty, skipped.");

                continue;
            }

            short gameEvent = result.Read<sbyte>(15);
            data.PoolId = result.Read<uint>(16);
            data.PhaseUseFlags = (PhaseUseFlagsValues)result.Read<byte>(17);
            data.PhaseId = result.Read<uint>(18);
            data.PhaseGroup = result.Read<uint>(19);

            if (Convert.ToBoolean(data.PhaseUseFlags & ~PhaseUseFlagsValues.All))
            {
                Log.Logger.Error("Table `gameobject` have gameobject (GUID: {0} Entry: {1}) has unknown `phaseUseFlags` set, removed unknown value.", guid, data.Id);
                data.PhaseUseFlags &= PhaseUseFlagsValues.All;
            }

            if (data.PhaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.AlwaysVisible) && data.PhaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.Inverse))
            {
                Log.Logger.Error("Table `gameobject` have gameobject (GUID: {0} Entry: {1}) has both `phaseUseFlags` PHASE_USE_FLAGS_ALWAYS_VISIBLE and PHASE_USE_FLAGS_INVERSE," +
                                 " removing PHASE_USE_FLAGS_INVERSE.",
                                 guid,
                                 data.Id);

                data.PhaseUseFlags &= ~PhaseUseFlagsValues.Inverse;
            }

            if (data.PhaseGroup != 0 && data.PhaseId != 0)
            {
                Log.Logger.Error("Table `gameobject` have gameobject (GUID: {0} Entry: {1}) with both `phaseid` and `phasegroup` set, `phasegroup` set to 0", guid, data.Id);
                data.PhaseGroup = 0;
            }

            if (data.PhaseId != 0)
                if (!_phaseRecords.ContainsKey(data.PhaseId))
                {
                    Log.Logger.Error("Table `gameobject` have gameobject (GUID: {0} Entry: {1}) with `phaseid` {2} does not exist, set to 0", guid, data.Id, data.PhaseId);
                    data.PhaseId = 0;
                }

            if (data.PhaseGroup != 0)
                if (_db2Manager.GetPhasesForGroup(data.PhaseGroup).Empty())
                {
                    Log.Logger.Error("Table `gameobject` have gameobject (GUID: {0} Entry: {1}) with `phaseGroup` {2} does not exist, set to 0", guid, data.Id, data.PhaseGroup);
                    data.PhaseGroup = 0;
                }

            data.TerrainSwapMap = result.Read<int>(20);

            if (data.TerrainSwapMap != -1)
            {
                if (!_mapRecords.TryGetValue((uint)data.TerrainSwapMap, out var terrainSwapEntry))
                {
                    Log.Logger.Error("Table `gameobject` have gameobject (GUID: {0} Entry: {1}) with `terrainSwapMap` {2} does not exist, set to -1", guid, data.Id, data.TerrainSwapMap);
                    data.TerrainSwapMap = -1;
                }
                else if (terrainSwapEntry.ParentMapID != data.MapId)
                {
                    Log.Logger.Error("Table `gameobject` have gameobject (GUID: {0} Entry: {1}) with `terrainSwapMap` {2} which cannot be used on spawn map, set to -1", guid, data.Id, data.TerrainSwapMap);
                    data.TerrainSwapMap = -1;
                }
            }

            data.ScriptId = _scriptManager.GetScriptId(result.Read<string>(21));

            if (data.Rotation.X is < -1.0f or > 1.0f)
            {
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) with invalid rotationX ({2}) value, skip", guid, data.Id, data.Rotation.X);

                continue;
            }

            if (data.Rotation.Y is < -1.0f or > 1.0f)
            {
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) with invalid rotationY ({2}) value, skip", guid, data.Id, data.Rotation.Y);

                continue;
            }

            if (data.Rotation.Z is < -1.0f or > 1.0f)
            {
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) with invalid rotationZ ({2}) value, skip", guid, data.Id, data.Rotation.Z);

                continue;
            }

            if (data.Rotation.W is < -1.0f or > 1.0f)
            {
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) with invalid rotationW ({2}) value, skip", guid, data.Id, data.Rotation.W);

                continue;
            }

            if (!_gridDefines.IsValidMapCoord(data.MapId, data.SpawnPoint))
            {
                Log.Logger.Error("Table `gameobject` has gameobject (GUID: {0} Entry: {1}) with invalid coordinates, skip", guid, data.Id);

                continue;
            }

            if (!(Math.Abs(Quaternion.Dot(data.Rotation, data.Rotation) - 1) < 1e-5))
            {
                Log.Logger.Error($"Table `gameobject` has gameobject (GUID: {guid} Entry: {data.Id}) with invalid rotation quaternion (non-unit), defaulting to orientation on Z axis only");
                data.Rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(data.SpawnPoint.Orientation, 0f, 0f));
            }

            if (_configuration.GetDefaultValue("Calculate:Gameoject:Zone:Area:Data", false))
            {
                _phasingHandler.InitDbVisibleMapId(phaseShift, data.TerrainSwapMap);
                _terrainManager.GetZoneAndAreaId(phaseShift, out var zoneId, out var areaId, data.MapId, data.SpawnPoint);

                var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.UPD_GAMEOBJECT_ZONE_AREA_DATA);
                stmt.AddValue(0, zoneId);
                stmt.AddValue(1, areaId);
                stmt.AddValue(2, guid);
                _worldDatabase.Execute(stmt);
            }

            // if not this is to be managed by GameEvent System
            if (gameEvent == 0)
                _mapObjectCache.AddSpawnDataToGrid(data);

            AllGameObjectData[guid] = data;
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} gameobjects in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }

    public GameObjectData NewOrExistGameObjectData(ulong spawnId)
    {
        if (!AllGameObjectData.ContainsKey(spawnId))
            AllGameObjectData[spawnId] = new GameObjectData();

        return AllGameObjectData[spawnId];
    }
}