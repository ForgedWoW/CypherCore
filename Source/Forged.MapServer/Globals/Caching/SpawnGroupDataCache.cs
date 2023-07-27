// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Maps;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class SpawnGroupDataCache : IObjectCache
{
    private readonly Dictionary<uint, SpawnGroupTemplateData> _spawnGroupDataStorage = new();
    private readonly WorldDatabase _worldDatabase;

    public SpawnGroupDataCache(WorldDatabase worldDatabase)
    {
        _worldDatabase = worldDatabase;
    }

    public SpawnGroupTemplateData GetDefaultSpawnGroup()
    {
        if (!_spawnGroupDataStorage.TryGetValue(0, out var gt))
            gt = _spawnGroupDataStorage.ElementAt(0).Value;

        return gt;
    }

    public SpawnGroupTemplateData GetLegacySpawnGroup()
    {
        if (!_spawnGroupDataStorage.TryGetValue(1, out var gt))
            gt = _spawnGroupDataStorage.ElementAt(1).Value;

        return gt;
    }

    public SpawnGroupTemplateData GetSpawnGroupData(uint groupId)
    {
        return _spawnGroupDataStorage.LookupByKey(groupId);
    }

    public bool TryGetSpawnGroupData(uint groupId, out SpawnGroupTemplateData spawnGroupTemplateData)
    {
        return _spawnGroupDataStorage.TryGetValue(groupId, out spawnGroupTemplateData);
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        //                                         0        1          2
        var result = _worldDatabase.Query("SELECT groupId, groupName, groupFlags FROM spawn_group_template");

        if (!result.IsEmpty())
            do
            {
                var groupId = result.Read<uint>(0);

                SpawnGroupTemplateData group = new()
                {
                    GroupId = groupId,
                    Name = result.Read<string>(1),
                    MapId = 0xFFFFFFFF
                };

                var flags = (SpawnGroupFlags)result.Read<uint>(2);

                if (flags.HasAnyFlag(~SpawnGroupFlags.All))
                {
                    flags &= SpawnGroupFlags.All;
                    Log.Logger.Error($"Invalid spawn group Id {flags} on group ID {groupId} ({group.Name}), reduced to valid Id {group.Flags}.");
                }

                if (flags.HasAnyFlag(SpawnGroupFlags.System) && flags.HasAnyFlag(SpawnGroupFlags.ManualSpawn))
                {
                    flags &= ~SpawnGroupFlags.ManualSpawn;
                    Log.Logger.Error($"System spawn group {groupId} ({group.Name}) has invalid manual spawn Id. Ignored.");
                }

                group.Flags = flags;

                _spawnGroupDataStorage[groupId] = group;
            } while (result.NextRow());

        if (!_spawnGroupDataStorage.ContainsKey(0))
        {
            Log.Logger.Error("Default spawn group (index 0) is missing from DB! Manually inserted.");

            SpawnGroupTemplateData data = new()
            {
                GroupId = 0,
                Name = "Default Group",
                MapId = 0,
                Flags = SpawnGroupFlags.System
            };

            _spawnGroupDataStorage[0] = data;
        }

        if (!_spawnGroupDataStorage.ContainsKey(1))
        {
            Log.Logger.Error("Default legacy spawn group (index 1) is missing from DB! Manually inserted.");

            SpawnGroupTemplateData data = new()
            {
                GroupId = 1,
                Name = "Legacy Group",
                MapId = 0,
                Flags = SpawnGroupFlags.System | SpawnGroupFlags.CompatibilityMode
            };

            _spawnGroupDataStorage[1] = data;
        }

        if (!result.IsEmpty())
            Log.Logger.Information($"Loaded {_spawnGroupDataStorage.Count} spawn group templates in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        else
            Log.Logger.Information("Loaded 0 spawn group templates. DB table `spawn_group_template` is empty.");
    }
}