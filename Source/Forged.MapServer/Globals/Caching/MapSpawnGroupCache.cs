// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Maps;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class MapSpawnGroupCache
{
    private readonly WorldDatabase _worldDatabase;
    private readonly IConfiguration _configuration;
    private readonly SpawnGroupDataCache _spawnGroupDataCache;
    private readonly SpawnDataCacheRouter _spawnDataCacheRouter;
    private readonly MultiMap<uint, uint> _spawnGroupsByMap = new();
    public MultiMap<uint, SpawnMetadata> SpawnGroupMapStorage { get; } = new();

    public MapSpawnGroupCache(WorldDatabase worldDatabase, IConfiguration configuration, SpawnGroupDataCache spawnGroupDataCache,
                              SpawnDataCacheRouter spawnDataCacheRouter)
    {
        _worldDatabase = worldDatabase;
        _configuration = configuration;
        _spawnGroupDataCache = spawnGroupDataCache;
        _spawnDataCacheRouter = spawnDataCacheRouter;
    }

    public List<uint> GetSpawnGroupsForMap(uint mapId)
    {
        return _spawnGroupsByMap.LookupByKey(mapId);
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        //                                         0        1          2
        var result = _worldDatabase.Query("SELECT groupId, spawnType, spawnId FROM spawn_group");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 spawn group members. DB table `spawn_group` is empty.");

            return;
        }

        uint numMembers = 0;

        do
        {
            var groupId = result.Read<uint>(0);
            var spawnType = (SpawnObjectType)result.Read<byte>(1);
            var spawnId = result.Read<ulong>(2);

            if (!SpawnMetadata.TypeIsValid(spawnType))
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM spawn_group WHERE groupId = {groupId} AND spawnType = {(byte)spawnType} AND spawnId = {spawnId}");
                else
                    Log.Logger.Error($"Spawn data with invalid type {spawnType} listed for spawn group {groupId}. Skipped.");

                continue;
            }

            var data = _spawnDataCacheRouter.GetSpawnMetadata(spawnType, spawnId);

            if (data == null)
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM spawn_group WHERE groupId = {groupId} AND spawnType = {(byte)spawnType} AND spawnId = {spawnId}");
                else
                    Log.Logger.Error($"Spawn data with ID ({spawnType},{spawnId}) not found, but is listed as a member of spawn group {groupId}!");

                continue;
            }

            if (data.SpawnGroupData.GroupId != 0)
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM spawn_group WHERE groupId = {groupId} AND spawnType = {(byte)spawnType} AND spawnId = {spawnId}");
                else
                    Log.Logger.Error($"Spawn with ID ({spawnType},{spawnId}) is listed as a member of spawn group {groupId}, but is already a member of spawn group {data.SpawnGroupData.GroupId}. Skipping.");

                continue;
            }

            if (!_spawnGroupDataCache.TryGetSpawnGroupData(groupId, out var groupTemplate))
            {
                Log.Logger.Error($"Spawn group {groupId} assigned to spawn ID ({spawnType},{spawnId}), but group is found!");
            }
            else
            {
                if (groupTemplate.MapId == 0xFFFFFFFF)
                {
                    groupTemplate.MapId = data.MapId;
                    _spawnGroupsByMap.Add(data.MapId, groupId);
                }
                else if (groupTemplate.MapId != data.MapId && !groupTemplate.Flags.HasAnyFlag(SpawnGroupFlags.System))
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM spawn_group WHERE groupId = {groupId} AND spawnType = {(byte)spawnType} AND spawnId = {spawnId}");
                    else
                        Log.Logger.Error($"Spawn group {groupId} has map ID {groupTemplate.MapId}, but spawn ({spawnType},{spawnId}) has map id {data.MapId} - spawn NOT added to group!");

                    continue;
                }

                data.SpawnGroupData = groupTemplate;

                if (!groupTemplate.Flags.HasAnyFlag(SpawnGroupFlags.System))
                    SpawnGroupMapStorage.Add(groupId, data);

                ++numMembers;
            }
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {numMembers} spawn group members in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void OnDeleteSpawnData(SpawnData data)
    {
        var templateIt = _spawnGroupDataCache.GetSpawnGroupData(data.SpawnGroupData.GroupId);

        if (templateIt.Flags.HasAnyFlag(SpawnGroupFlags.System)) // system groups don't store their members in the map
            return;

        var spawnDatas = SpawnGroupMapStorage.LookupByKey(data.SpawnGroupData.GroupId);

        foreach (var it in spawnDatas)
        {
            if (it != data)
                continue;

            SpawnGroupMapStorage.Remove(data.SpawnGroupData.GroupId, it);

            return;
        }
    }
}