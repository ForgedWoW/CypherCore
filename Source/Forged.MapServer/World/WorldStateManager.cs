// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking.Packets.WorldState;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IWorldState;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.World;

public class WorldStateManager
{
    private const int AnyMap = -1;
    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDB;
    private readonly DB2Manager _db2Manager;
    private readonly GameObjectManager _objectManager;
    private readonly Dictionary<int, int> _realmWorldStateValues = new();
    private readonly ScriptManager _scriptManager;
    private readonly WorldDatabase _worldDatabase;
    private readonly WorldManager _worldManager;
    private readonly Dictionary<int, Dictionary<int, int>> _worldStatesByMap = new();
    private readonly Dictionary<int, WorldStateTemplate> _worldStateTemplates = new();
    public WorldStateManager(IConfiguration configuration, WorldDatabase worldDatabase, CliDB cliDB, GameObjectManager objectManager,
                             CharacterDatabase characterDatabase, ScriptManager scriptManager, WorldManager worldManager, DB2Manager db2Manager)
    {
        _worldDatabase = worldDatabase;
        _cliDB = cliDB;
        _objectManager = objectManager;
        _characterDatabase = characterDatabase;
        _scriptManager = scriptManager;
        _worldManager = worldManager;
        _db2Manager = db2Manager;
        SetValue(WorldStates.CurrentPvpSeasonId, configuration.GetDefaultValue("Arena:ArenaSeason:InProgress", false) ? configuration.GetDefaultValue("Arena:ArenaSeason:ID", 32) : 0, false, null);
        SetValue(WorldStates.PreviousPvpSeasonId, configuration.GetDefaultValue("Arena:ArenaSeason:ID", 32) - (configuration.GetDefaultValue("Arena:ArenaSeason:InProgress", false) ? 1 : 0), false, null);
    }

    public void FillInitialWorldStates(InitWorldStates initWorldStates, Map map, uint playerAreaId)
    {
        foreach (var (worldStateId, value) in _realmWorldStateValues)
            initWorldStates.AddState(worldStateId, value);

        foreach (var (worldStateId, value) in map.GetWorldStateValues())
        {
            var worldStateTemplate = GetWorldStateTemplate(worldStateId);

            if (worldStateTemplate != null && !worldStateTemplate.AreaIds.Empty())
            {
                var isInAllowedArea = worldStateTemplate.AreaIds.Any(requiredAreaId => _db2Manager.IsInArea(playerAreaId, requiredAreaId));

                if (!isInAllowedArea)
                    continue;
            }

            initWorldStates.AddState(worldStateId, value);
        }
    }

    public Dictionary<int, int> GetInitialWorldStatesForMap(Map map)
    {
        Dictionary<int, int> initialValues = new();

        if (_worldStatesByMap.TryGetValue((int)map.Id, out var valuesTemplate))
            foreach (var (key, value) in valuesTemplate)
                initialValues.Add(key, value);

        if (_worldStatesByMap.TryGetValue(AnyMap, out valuesTemplate))
            foreach (var (key, value) in valuesTemplate)
                initialValues.Add(key, value);

        return initialValues;
    }

    public int GetValue(WorldStates worldStateId, Map map)
    {
        return GetValue((int)worldStateId, map);
    }

    public int GetValue(int worldStateId, Map map)
    {
        var worldStateTemplate = GetWorldStateTemplate(worldStateId);

        if (worldStateTemplate == null || worldStateTemplate.MapIds.Empty())
            return _realmWorldStateValues.LookupByKey(worldStateId);

        if (map == null || (!worldStateTemplate.MapIds.Contains((int)map.Id) && !worldStateTemplate.MapIds.Contains(AnyMap)))
            return 0;

        return map.GetWorldStateValue(worldStateId);
    }

    public WorldStateTemplate GetWorldStateTemplate(int worldStateId)
    {
        return _worldStateTemplates.LookupByKey(worldStateId);
    }

    public void LoadFromDB()
    {
        var oldMSTime = Time.MSTime;

        //                                         0   1             2       3        4
        var result = _worldDatabase.Query("SELECT ID, DefaultValue, MapIDs, AreaIDs, ScriptName FROM world_state");

        if (result.IsEmpty())
            return;

        do
        {
            var id = result.Read<int>(0);

            WorldStateTemplate worldState = new()
            {
                Id = id,
                DefaultValue = result.Read<int>(1)
            };

            var mapIds = result.Read<string>(2);

            if (!mapIds.IsEmpty())
                foreach (string mapIdToken in new StringArray(mapIds, ','))
                {
                    if (!int.TryParse(mapIdToken, out var mapId))
                    {
                        Log.Logger.Error($"Table `world_state` contains a world state {id} with non-integer MapID ({mapIdToken}), map ignored");

                        continue;
                    }

                    if (mapId != AnyMap && !_cliDB.MapStorage.ContainsKey(mapId))
                    {
                        Log.Logger.Error($"Table `world_state` contains a world state {id} with invalid MapID ({mapId}), map ignored");

                        continue;
                    }

                    worldState.MapIds.Add(mapId);
                }

            if (!mapIds.IsEmpty() && worldState.MapIds.Empty())
            {
                Log.Logger.Error($"Table `world_state` contains a world state {id} with nonempty MapIDs ({mapIds}) but no valid map id was found, ignored");

                continue;
            }

            var areaIds = result.Read<string>(3);

            if (!areaIds.IsEmpty() && !worldState.MapIds.Empty())
            {
                foreach (string areaIdToken in new StringArray(areaIds, ','))
                {
                    if (!uint.TryParse(areaIdToken, out var areaId))
                    {
                        Log.Logger.Error($"Table `world_state` contains a world state {id} with non-integer AreaID ({areaIdToken}), area ignored");

                        continue;
                    }

                    if (!_cliDB.AreaTableStorage.TryGetValue(areaId, out var areaTableEntry))
                    {
                        Log.Logger.Error($"Table `world_state` contains a world state {id} with invalid AreaID ({areaId}), area ignored");

                        continue;
                    }

                    if (!worldState.MapIds.Contains(areaTableEntry.ContinentID))
                    {
                        Log.Logger.Error($"Table `world_state` contains a world state {id} with AreaID ({areaId}) not on any of required maps, area ignored");

                        continue;
                    }

                    worldState.AreaIds.Add(areaId);
                }

                if (!areaIds.IsEmpty() && worldState.AreaIds.Empty())
                {
                    Log.Logger.Error($"Table `world_state` contains a world state {id} with nonempty AreaIDs ({areaIds}) but no valid area id was found, ignored");

                    continue;
                }
            }
            else if (!areaIds.IsEmpty())
            {
                Log.Logger.Error($"Table `world_state` contains a world state {id} with nonempty AreaIDs ({areaIds}) but is a realm wide world state, area requirement ignored");
            }

            worldState.ScriptId = _objectManager.GetScriptId(result.Read<string>(4));

            if (!worldState.MapIds.Empty())
                foreach (var mapId in worldState.MapIds)
                {
                    if (!_worldStatesByMap.ContainsKey(mapId))
                        _worldStatesByMap[mapId] = new Dictionary<int, int>();

                    _worldStatesByMap[mapId][id] = worldState.DefaultValue;
                }
            else
                _realmWorldStateValues[id] = worldState.DefaultValue;

            _worldStateTemplates[id] = worldState;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {_worldStateTemplates.Count} world state templates {Time.GetMSTimeDiffToNow(oldMSTime)} ms");

        oldMSTime = Time.MSTime;

        result = _characterDatabase.Query("SELECT Id, Value FROM world_state_value");
        uint savedValueCount = 0;

        if (!result.IsEmpty())
            do
            {
                var worldStateId = result.Read<int>(0);
                if (!_worldStateTemplates.TryGetValue(worldStateId, out var worldState))
                {
                    Log.Logger.Error($"Table `world_state_value` contains a value for unknown world state {worldStateId}, ignored");

                    continue;
                }

                var value = result.Read<int>(1);

                if (!worldState.MapIds.Empty())
                    foreach (var mapId in worldState.MapIds)
                    {
                        if (!_worldStatesByMap.ContainsKey(mapId))
                            _worldStatesByMap[mapId] = new Dictionary<int, int>();

                        _worldStatesByMap[mapId][worldStateId] = value;
                    }
                else
                    _realmWorldStateValues[worldStateId] = value;

                ++savedValueCount;
            } while (result.NextRow());

        Log.Logger.Information($"Loaded {savedValueCount} saved world state values {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }
    public void SaveValueInDb(int worldStateId, int value)
    {
        if (GetWorldStateTemplate(worldStateId) == null)
            return;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.REP_WORLD_STATE);
        stmt.AddValue(0, worldStateId);
        stmt.AddValue(1, value);
        _characterDatabase.Execute(stmt);
    }

    public void SetForcedWarModeFactionBalanceState(int team, int reward = 0)
    {
        SetValueAndSaveInDb(WorldStates.WarModeHordeBuffValue, 10 + (team == TeamIds.Alliance ? reward : 0), false, null);
        SetValueAndSaveInDb(WorldStates.WarModeAllianceBuffValue, 10 + (team == TeamIds.Horde ? reward : 0), false, null);
    }

    public void SetValue(WorldStates worldStateId, int value, bool hidden, Map map)
    {
        SetValue((int)worldStateId, value, hidden, map);
    }

    public void SetValue(uint worldStateId, int value, bool hidden, Map map)
    {
        SetValue((int)worldStateId, value, hidden, map);
    }

    public void SetValue(int worldStateId, int value, bool hidden, Map map)
    {
        var worldStateTemplate = GetWorldStateTemplate(worldStateId);

        if (worldStateTemplate == null || worldStateTemplate.MapIds.Empty())
        {
            var oldValue = 0;

            if (!_realmWorldStateValues.TryAdd(worldStateId, 0))
            {
                oldValue = _realmWorldStateValues[worldStateId];

                if (oldValue == value)
                    return;
            }

            _realmWorldStateValues[worldStateId] = value;

            if (worldStateTemplate != null)
                _scriptManager.RunScript<IWorldStateOnValueChange>(script => script.OnValueChange(worldStateTemplate.Id, oldValue, value, null), worldStateTemplate.ScriptId);

            // Broadcast update to all players on the server
            UpdateWorldState updateWorldState = new()
            {
                VariableID = (uint)worldStateId,
                Value = value,
                Hidden = hidden
            };

            _worldManager.SendGlobalMessage(updateWorldState);

            return;
        }

        if (map == null || (!worldStateTemplate.MapIds.Contains((int)map.Id) && !worldStateTemplate.MapIds.Contains(AnyMap)))
            return;

        map.SetWorldStateValue(worldStateId, value, hidden);
    }
    public void SetValueAndSaveInDb(WorldStates worldStateId, int value, bool hidden, Map map)
    {
        SetValueAndSaveInDb((int)worldStateId, value, hidden, map);
    }

    public void SetValueAndSaveInDb(int worldStateId, int value, bool hidden, Map map)
    {
        SetValue(worldStateId, value, hidden, map);
        SaveValueInDb(worldStateId, value);
    }
}