// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.D;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.DataStorage.Structs.P;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps;
using Forged.MapServer.Phasing;
using Forged.MapServer.Scripting;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class CreatureDataCache : IObjectCache
{
    private readonly CreatureTemplateCache _creatureTemplateCache;
    private readonly WorldDatabase _worldDatabase;
    private readonly DB2Manager _db2Manager;
    private readonly IConfiguration _configuration;
    private readonly ScriptManager _scriptManager;
    private readonly SpawnGroupDataCache _spawnGroupDataCache;
    private readonly GameObjectTemplateCache _gameObjectTemplateCache;
    private readonly DB6Storage<MapRecord> _mapRecords;
    private readonly DB6Storage<PhaseRecord> _phaseRecords;
    private readonly PhasingHandler _phasingHandler;
    private readonly TerrainManager _terrainManager;
    private readonly DB6Storage<DifficultyRecord> _difficultyRecords;
    private readonly EquipmentInfoCache _equipmentInfoCache;
    private readonly GameObjectCache _gameObjectCache;
    private readonly MapObjectCache _mapObjectCache;
    private readonly MapSpawnGroupCache _mapSpawnGroupCache;
    private readonly Dictionary<ObjectGuid, ObjectGuid> _linkedRespawnStorage = new();

    public Dictionary<ulong, CreatureData> AllCreatureData { get; } = new();

    public CreatureDataCache(CreatureTemplateCache creatureTemplateCache, WorldDatabase worldDatabase, DB2Manager db2Manager,
                             IConfiguration configuration, ScriptManager scriptManager, SpawnGroupDataCache spawnGroupDataCache,
                             GameObjectTemplateCache gameObjectTemplateCache, DB6Storage<MapRecord> mapRecords, DB6Storage<PhaseRecord> phaseRecords,
                             PhasingHandler phasingHandler, TerrainManager terrainManager, DB6Storage<DifficultyRecord> difficultyRecords,
                             EquipmentInfoCache equipmentInfoCache, GameObjectCache gameObjectCache, MapObjectCache mapObjectCache, MapSpawnGroupCache mapSpawnGroupCache)
    {
        _creatureTemplateCache = creatureTemplateCache;
        _worldDatabase = worldDatabase;
        _db2Manager = db2Manager;
        _configuration = configuration;
        _scriptManager = scriptManager;
        _spawnGroupDataCache = spawnGroupDataCache;
        _gameObjectTemplateCache = gameObjectTemplateCache;
        _mapRecords = mapRecords;
        _phaseRecords = phaseRecords;
        _phasingHandler = phasingHandler;
        _terrainManager = terrainManager;
        _difficultyRecords = difficultyRecords;
        _equipmentInfoCache = equipmentInfoCache;
        _gameObjectCache = gameObjectCache;
        _mapObjectCache = mapObjectCache;
        _mapSpawnGroupCache = mapSpawnGroupCache;
    }

    public void Load()
    {
        LoadCreatureData();
        LoadLinkedRespawn();
    }

    public void DeleteCreatureData(ulong spawnId)
    {
        var data = GetCreatureData(spawnId);

        if (data != null)
        {
            _mapObjectCache.RemoveSpawnDataFromGrid(data);
            _mapSpawnGroupCache.OnDeleteSpawnData(data);
        }

        AllCreatureData.Remove(spawnId);
    }

    public CreatureData GetCreatureData(ulong spawnId)
    {
        return AllCreatureData.LookupByKey(spawnId);
    }

    public ObjectGuid GetLinkedRespawnGuid(ObjectGuid spawnId)
    {
        return !_linkedRespawnStorage.TryGetValue(spawnId, out var retGuid) ? ObjectGuid.Empty : retGuid;
    }

    public void LoadCreatureData()
    {
        var time = Time.MSTime;

        //                                         0              1   2    3           4           5           6            7        8             9              10
        var result = _worldDatabase.Query("SELECT creature.guid, id, map, position_x, position_y, position_z, orientation, modelid, equipment_id, spawntimesecs, wander_distance, " +
                                          //11               12         13       14            15                 16          17           18                19                   20                    21
                                          "currentwaypoint, curhealth, curmana, MovementType, spawnDifficulties, eventEntry, poolSpawnId, creature.npcflag, creature.unit_flags, creature.unit_flags2, creature.unit_flags3, " +
                                          //   22                     23                      24                25                   26                       27                   28
                                          "creature.dynamicflags, creature.phaseUseFlags, creature.phaseid, creature.phasegroup, creature.terrainSwapMap, creature.ScriptName, creature.StringId " +
                                          "FROM creature LEFT OUTER JOIN game_event_creature ON creature.guid = game_event_creature.guid LEFT OUTER JOIN pool_members ON pool_members.type = 0 AND creature.guid = pool_members.spawnId");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creatures. DB table `creature` is empty.");

            return;
        }

        // Build single time for check spawnmask
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

        uint count = 0;

        do
        {
            var guid = result.Read<ulong>(0);
            var entry = result.Read<uint>(1);

            var cInfo = _creatureTemplateCache.GetCreatureTemplate(entry);

            if (cInfo == null)
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM creature WHERE id = {entry}");
                else
                    Log.Logger.Error("Table `creature` has creature (GUID: {0}) with non existing creature entry {1}, skipped.", guid, entry);

                continue;
            }

            CreatureData data = new()
            {
                SpawnId = guid,
                Id = entry,
                MapId = result.Read<ushort>(2),
                SpawnPoint = new Position(result.Read<float>(3), result.Read<float>(4), result.Read<float>(5), result.Read<float>(6)),
                Displayid = result.Read<uint>(7),
                EquipmentId = result.Read<sbyte>(8),
                Spawntimesecs = result.Read<int>(9),
                WanderDistance = result.Read<float>(10),
                Currentwaypoint = result.Read<uint>(11),
                Curhealth = result.Read<uint>(12),
                Curmana = result.Read<uint>(13),
                MovementType = result.Read<byte>(14)
            };

            data.SpawnDifficulties = ParseSpawnDifficulties(result.Read<string>(15), "creature", guid, data.MapId, spawnMasks.LookupByKey(data.MapId));
            var gameEvent = result.Read<short>(16);
            data.PoolId = result.Read<uint>(17);
            data.Npcflag = result.Read<ulong>(18);
            data.UnitFlags = result.Read<uint>(19);
            data.UnitFlags2 = result.Read<uint>(20);
            data.UnitFlags3 = result.Read<uint>(21);
            data.Dynamicflags = result.Read<uint>(22);
            data.PhaseUseFlags = (PhaseUseFlagsValues)result.Read<byte>(23);
            data.PhaseId = result.Read<uint>(24);
            data.PhaseGroup = result.Read<uint>(25);
            data.TerrainSwapMap = result.Read<int>(26);

            var scriptId = result.Read<string>(27);

            if (string.IsNullOrEmpty(scriptId))
                data.ScriptId = _scriptManager.GetScriptId(scriptId);

            data.StringId = result.Read<string>(28);
            data.SpawnGroupData = _spawnGroupDataCache.GetSpawnGroupData(_gameObjectTemplateCache.IsTransportMap(data.MapId) ? 1 : 0u); // transport spawns default to compatibility group

            if (!_mapRecords.TryGetValue(data.MapId, out var mapEntry))
            {
                Log.Logger.Error("Table `creature` have creature (GUID: {0}) that spawned at not existed map (Id: {1}), skipped.", guid, data.MapId);

                continue;
            }

            if (data.SpawnDifficulties.Empty())
            {
                Log.Logger.Error($"Table `creature` has creature (GUID: {guid}) that is not spawned in any difficulty, skipped.");

                continue;
            }

            var ok = true;

            for (uint diff = 0; diff < SharedConst.MaxCreatureDifficulties && ok; ++diff)
                if (_creatureTemplateCache.DifficultyEntries[diff].Contains(data.Id))
                {
                    Log.Logger.Error("Table `creature` have creature (GUID: {0}) that listed as difficulty {1} template (entry: {2}) in `creaturetemplate`, skipped.", guid, diff + 1, data.Id);
                    ok = false;
                }

            if (!ok)
                continue;

            // -1 random, 0 no equipment,
            if (data.EquipmentId != 0)
                if (_equipmentInfoCache.GetEquipmentInfo(data.Id, data.EquipmentId) == null)
                {
                    Log.Logger.Error("Table `creature` have creature (Entry: {0}) with equipmentid {1} not found in table `creatureequiptemplate`, set to no equipment.", data.Id, data.EquipmentId);
                    data.EquipmentId = 0;
                }

            if (cInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.InstanceBind))
                if (!mapEntry.IsDungeon())
                    Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `creature_template`.`flagsextra` including CREATUREFLAGEXTRAINSTANCEBIND " +
                                     "but creature are not in instance.",
                                     guid,
                                     data.Id);

            if (data.WanderDistance < 0.0f)
            {
                Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `wander_distance`< 0, set to 0.", guid, data.Id);
                data.WanderDistance = 0.0f;
            }
            else if (data.MovementType == (byte)MovementGeneratorType.Random)
            {
                if (MathFunctions.fuzzyEq(data.WanderDistance, 0.0f))
                {
                    Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `MovementType`=1 (random movement) but with `wander_distance`=0, replace by idle movement type (0).", guid, data.Id);
                    data.MovementType = (byte)MovementGeneratorType.Idle;
                }
            }
            else if (data.MovementType == (byte)MovementGeneratorType.Idle)
            {
                if (data.WanderDistance != 0.0f)
                {
                    Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `MovementType`=0 (idle) have `wander_distance`<>0, set to 0.", guid, data.Id);
                    data.WanderDistance = 0.0f;
                }
            }

            if (Convert.ToBoolean(data.PhaseUseFlags & ~PhaseUseFlagsValues.All))
            {
                Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) has unknown `phaseUseFlags` set, removed unknown value.", guid, data.Id);
                data.PhaseUseFlags &= PhaseUseFlagsValues.All;
            }

            if (data.PhaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.AlwaysVisible) && data.PhaseUseFlags.HasAnyFlag(PhaseUseFlagsValues.Inverse))
            {
                Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) has both `phaseUseFlags` PHASE_USE_FLAGS_ALWAYS_VISIBLE and PHASE_USE_FLAGS_INVERSE," +
                                 " removing PHASE_USE_FLAGS_INVERSE.",
                                 guid,
                                 data.Id);

                data.PhaseUseFlags &= ~PhaseUseFlagsValues.Inverse;
            }

            if (data.PhaseGroup != 0 && data.PhaseId != 0)
            {
                Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with both `phaseid` and `phasegroup` set, `phasegroup` set to 0", guid, data.Id);
                data.PhaseGroup = 0;
            }

            if (data.PhaseId != 0)
                if (!_phaseRecords.ContainsKey(data.PhaseId))
                {
                    Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `phaseid` {2} does not exist, set to 0", guid, data.Id, data.PhaseId);
                    data.PhaseId = 0;
                }

            if (data.PhaseGroup != 0)
                if (_db2Manager.GetPhasesForGroup(data.PhaseGroup).Empty())
                {
                    Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `phasegroup` {2} does not exist, set to 0", guid, data.Id, data.PhaseGroup);
                    data.PhaseGroup = 0;
                }

            if (data.TerrainSwapMap != -1)
            {
                if (!_mapRecords.TryGetValue((uint)data.TerrainSwapMap, out var terrainSwapEntry))
                {
                    Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `terrainSwapMap` {2} does not exist, set to -1", guid, data.Id, data.TerrainSwapMap);
                    data.TerrainSwapMap = -1;
                }
                else if (terrainSwapEntry.ParentMapID != data.MapId)
                {
                    Log.Logger.Error("Table `creature` have creature (GUID: {0} Entry: {1}) with `terrainSwapMap` {2} which cannot be used on spawn map, set to -1", guid, data.Id, data.TerrainSwapMap);
                    data.TerrainSwapMap = -1;
                }
            }

            var disallowedUnitFlags = (uint)(cInfo.UnitFlags & ~UnitFlags.Allowed);

            if (disallowedUnitFlags != 0)
            {
                Log.Logger.Error($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with disallowed `unit_flags` {disallowedUnitFlags}, removing incorrect Id.");
                cInfo.UnitFlags &= UnitFlags.Allowed;
            }

            var disallowedUnitFlags2 = cInfo.UnitFlags2 & ~(uint)UnitFlags2.Allowed;

            if (disallowedUnitFlags2 != 0)
            {
                Log.Logger.Error($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with disallowed `unit_flags2` {disallowedUnitFlags2}, removing incorrect Id.");
                cInfo.UnitFlags2 &= (uint)UnitFlags2.Allowed;
            }

            var disallowedUnitFlags3 = cInfo.UnitFlags3 & ~(uint)UnitFlags3.Allowed;

            if (disallowedUnitFlags3 != 0)
            {
                Log.Logger.Error($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with disallowed `unit_flags2` {disallowedUnitFlags3}, removing incorrect Id.");
                cInfo.UnitFlags3 &= (uint)UnitFlags3.Allowed;
            }

            if (cInfo.DynamicFlags != 0)
            {
                Log.Logger.Error($"Table `creature_template` lists creature (Entry: {cInfo.Entry}) with `dynamicflags` > 0. Ignored and set to 0.");
                cInfo.DynamicFlags = 0;
            }

            if (_configuration.GetDefaultValue("Calculate:Creature:Zone:Area:Data", false))
            {
                _phasingHandler.InitDbVisibleMapId(phaseShift, data.TerrainSwapMap);
                _terrainManager.GetZoneAndAreaId(phaseShift, out var zoneId, out var areaId, data.MapId, data.SpawnPoint);

                var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.UPD_CREATURE_ZONE_AREA_DATA);
                stmt.AddValue(0, zoneId);
                stmt.AddValue(1, areaId);
                stmt.AddValue(2, guid);

                _worldDatabase.Execute(stmt);
            }

            // Add to grid if not managed by the GameInfo event
            if (gameEvent == 0)
                _mapObjectCache.AddSpawnDataToGrid(data);

            AllCreatureData[guid] = data;
            count++;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} creatures in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }

    public void LoadLinkedRespawn()
    {
        var oldMSTime = Time.MSTime;

        _linkedRespawnStorage.Clear();
        //                                                 0        1          2
        var result = _worldDatabase.Query("SELECT guid, linkedGuid, linkType FROM linked_respawn ORDER BY guid ASC");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 linked respawns. DB table `linked_respawn` is empty.");

            return;
        }

        do
        {
            var guidLow = result.Read<ulong>(0);
            var linkedGuidLow = result.Read<ulong>(1);
            var linkType = result.Read<byte>(2);

            var guid = ObjectGuid.Empty;
            var linkedGuid = ObjectGuid.Empty;
            var error = false;

            switch ((CreatureLinkedRespawnType)linkType)
            {
                case CreatureLinkedRespawnType.CreatureToCreature:
                {
                    var slave = GetCreatureData(guidLow);

                    if (slave == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get creature data for GUIDLow {0}", guidLow);

                        error = true;

                        break;
                    }

                    var master = GetCreatureData(linkedGuidLow);

                    if (master == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get creature data for GUIDLow {0}", linkedGuidLow);

                        error = true;

                        break;
                    }

                    var map = _mapRecords.LookupByKey(master.MapId);

                    if (map == null || !map.Instanceable() || master.MapId != slave.MapId)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Creature '{0}' linking to '{1}' on an unpermitted map.", guidLow, linkedGuidLow);

                        error = true;

                        break;
                    }

                    // they must have a possibility to meet (normal/heroic difficulty)
                    if (!master.SpawnDifficulties.Intersect(slave.SpawnDifficulties).Any())
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("LinkedRespawn: Creature '{0}' linking to '{1}' with not corresponding spawnMask", guidLow, linkedGuidLow);

                        error = true;

                        break;
                    }

                    guid = ObjectGuid.Create(HighGuid.Creature, slave.MapId, slave.Id, guidLow);
                    linkedGuid = ObjectGuid.Create(HighGuid.Creature, master.MapId, master.Id, linkedGuidLow);

                    break;
                }
                case CreatureLinkedRespawnType.CreatureToGO:
                {
                    var slave = GetCreatureData(guidLow);

                    if (slave == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get creature data for GUIDLow {0}", guidLow);

                        error = true;

                        break;
                    }

                    var master = _gameObjectCache.GetGameObjectData(linkedGuidLow);

                    if (master == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get gameobject data for GUIDLow {0}", linkedGuidLow);

                        error = true;

                        break;
                    }

                    var map = _mapRecords.LookupByKey(master.MapId);

                    if (map == null || !map.Instanceable() || master.MapId != slave.MapId)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Creature '{0}' linking to '{1}' on an unpermitted map.", guidLow, linkedGuidLow);

                        error = true;

                        break;
                    }

                    // they must have a possibility to meet (normal/heroic difficulty)
                    if (!master.SpawnDifficulties.Intersect(slave.SpawnDifficulties).Any())
                    {
                        Log.Logger.Error("LinkedRespawn: Creature '{0}' linking to '{1}' with not corresponding spawnMask", guidLow, linkedGuidLow);
                        error = true;

                        break;
                    }

                    guid = ObjectGuid.Create(HighGuid.Creature, slave.MapId, slave.Id, guidLow);
                    linkedGuid = ObjectGuid.Create(HighGuid.GameObject, master.MapId, master.Id, linkedGuidLow);

                    break;
                }
                case CreatureLinkedRespawnType.GOToGO:
                {
                    var slave = _gameObjectCache.GetGameObjectData(guidLow);

                    if (slave == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get gameobject data for GUIDLow {0}", guidLow);

                        error = true;

                        break;
                    }

                    var master = _gameObjectCache.GetGameObjectData(linkedGuidLow);

                    if (master == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get gameobject data for GUIDLow {0}", linkedGuidLow);

                        error = true;

                        break;
                    }

                    var map = _mapRecords.LookupByKey(master.MapId);

                    if (map == null || !map.Instanceable() || master.MapId != slave.MapId)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Creature '{0}' linking to '{1}' on an unpermitted map.", guidLow, linkedGuidLow);

                        error = true;

                        break;
                    }

                    // they must have a possibility to meet (normal/heroic difficulty)
                    if (!master.SpawnDifficulties.Intersect(slave.SpawnDifficulties).Any())
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("LinkedRespawn: Creature '{0}' linking to '{1}' with not corresponding spawnMask", guidLow, linkedGuidLow);

                        error = true;

                        break;
                    }

                    guid = ObjectGuid.Create(HighGuid.GameObject, slave.MapId, slave.Id, guidLow);
                    linkedGuid = ObjectGuid.Create(HighGuid.GameObject, master.MapId, master.Id, linkedGuidLow);

                    break;
                }
                case CreatureLinkedRespawnType.GOToCreature:
                {
                    var slave = _gameObjectCache.GetGameObjectData(guidLow);

                    if (slave == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get gameobject data for GUIDLow {0}", guidLow);

                        error = true;

                        break;
                    }

                    var master = GetCreatureData(linkedGuidLow);

                    if (master == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Couldn't get creature data for GUIDLow {0}", linkedGuidLow);

                        error = true;

                        break;
                    }

                    var map = _mapRecords.LookupByKey(master.MapId);

                    if (map == null || !map.Instanceable() || master.MapId != slave.MapId)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("Creature '{0}' linking to '{1}' on an unpermitted map.", guidLow, linkedGuidLow);

                        error = true;

                        break;
                    }

                    // they must have a possibility to meet (normal/heroic difficulty)
                    if (!master.SpawnDifficulties.Intersect(slave.SpawnDifficulties).Any())
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM linked_respawn WHERE guid = {guidLow}");
                        else
                            Log.Logger.Error("LinkedRespawn: Creature '{0}' linking to '{1}' with not corresponding spawnMask", guidLow, linkedGuidLow);

                        error = true;

                        break;
                    }

                    guid = ObjectGuid.Create(HighGuid.GameObject, slave.MapId, slave.Id, guidLow);
                    linkedGuid = ObjectGuid.Create(HighGuid.Creature, master.MapId, master.Id, linkedGuidLow);

                    break;
                }
            }

            if (!error)
                _linkedRespawnStorage[guid] = linkedGuid;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} linked respawns in {1} ms", _linkedRespawnStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public CreatureData NewOrExistCreatureData(ulong spawnId)
    {
        if (!AllCreatureData.ContainsKey(spawnId))
            AllCreatureData[spawnId] = new CreatureData();

        return AllCreatureData[spawnId];
    }

    public bool SetCreatureLinkedRespawn(ulong guidLow, ulong linkedGuidLow)
    {
        if (guidLow == 0)
            return false;

        var master = GetCreatureData(guidLow);
        var guid = ObjectGuid.Create(HighGuid.Creature, master.MapId, master.Id, guidLow);
        PreparedStatement stmt;

        if (linkedGuidLow == 0) // we're removing the linking
        {
            _linkedRespawnStorage.Remove(guid);
            stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
            stmt.AddValue(0, guidLow);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.CreatureToCreature);
            _worldDatabase.Execute(stmt);

            return true;
        }

        var slave = GetCreatureData(linkedGuidLow);

        if (slave == null)
        {
            Log.Logger.Error("Creature '{0}' linking to non-existent creature '{1}'.", guidLow, linkedGuidLow);

            return false;
        }

        var map = _mapRecords.LookupByKey(master.MapId);

        if (map == null || !map.Instanceable() || master.MapId != slave.MapId)
        {
            Log.Logger.Error("Creature '{0}' linking to '{1}' on an unpermitted map.", guidLow, linkedGuidLow);

            return false;
        }

        // they must have a possibility to meet (normal/heroic difficulty)
        if (!master.SpawnDifficulties.Intersect(slave.SpawnDifficulties).Any())
        {
            Log.Logger.Error("LinkedRespawn: Creature '{0}' linking to '{1}' with not corresponding spawnMask", guidLow, linkedGuidLow);

            return false;
        }

        var linkedGuid = ObjectGuid.Create(HighGuid.Creature, slave.MapId, slave.Id, linkedGuidLow);

        _linkedRespawnStorage[guid] = linkedGuid;
        stmt = _worldDatabase.GetPreparedStatement(WorldStatements.REP_LINKED_RESPAWN);
        stmt.AddValue(0, guidLow);
        stmt.AddValue(1, linkedGuidLow);
        stmt.AddValue(2, (uint)CreatureLinkedRespawnType.CreatureToCreature);
        _worldDatabase.Execute(stmt);

        return true;
    }

    public List<Difficulty> ParseSpawnDifficulties(string difficultyString, string table, ulong spawnId, uint mapId, List<Difficulty> mapDifficulties)
    {
        List<Difficulty> difficulties = new();
        StringArray tokens = new(difficultyString, ',');

        if (tokens.Length == 0)
            return difficulties;

        var isTransportMap = _gameObjectTemplateCache.IsTransportMap(mapId);

        foreach (string token in tokens)
        {
            var difficultyId = (Difficulty)Enum.Parse(typeof(Difficulty), token);

            if (difficultyId != 0 && !_difficultyRecords.ContainsKey(difficultyId))
            {
                Log.Logger.Error($"Table `{table}` has {table} (GUID: {spawnId}) with non invalid difficulty id {difficultyId}, skipped.");

                continue;
            }

            if (!isTransportMap && !mapDifficulties.Contains(difficultyId))
            {
                Log.Logger.Error($"Table `{table}` has {table} (GUID: {spawnId}) has unsupported difficulty {difficultyId} for map (Id: {mapId}).");

                continue;
            }

            difficulties.Add(difficultyId);
        }

        difficulties.Sort();

        return difficulties;
    }
}