// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Grids;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class WorldSafeLocationsCache : IObjectCache
{
    private readonly WorldDatabase _worldDatabase;
    private readonly GridDefines _gridDefines;

    public WorldSafeLocationsCache(WorldDatabase worldDatabase, GridDefines gridDefines)
    {
        _worldDatabase = worldDatabase;
        _gridDefines = gridDefines;
    }

    public Dictionary<uint, WorldSafeLocsEntry> WorldSafeLocs { get; } = new();

    public WorldSafeLocsEntry GetWorldSafeLoc(uint id)
    {
        return WorldSafeLocs.LookupByKey(id);
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        //                                         0   1      2     3     4     5
        var result = _worldDatabase.Query("SELECT ID, MapID, LocX, LocY, LocZ, Facing FROM world_safe_locs");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 world locations. DB table `world_safe_locs` is empty.");

            return;
        }

        do
        {
            var id = result.Read<uint>(0);
            WorldLocation loc = new(result.Read<uint>(1), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), MathFunctions.DegToRad(result.Read<float>(5)));

            if (!_gridDefines.IsValidMapCoord(loc))
            {
                Log.Logger.Error($"World location (ID: {id}) has a invalid position MapID: {loc.MapId} {loc}, skipped");

                continue;
            }

            WorldSafeLocsEntry worldSafeLocs = new()
            {
                Id = id,
                Location = loc
            };

            WorldSafeLocs[id] = worldSafeLocs;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {WorldSafeLocs.Count} world locations {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }
}