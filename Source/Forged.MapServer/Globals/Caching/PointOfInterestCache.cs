// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Maps.Grids;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class PointOfInterestCache : IObjectCache
{
    private readonly GridDefines _gridDefines;
    private readonly Dictionary<uint, PointOfInterest> _pointsOfInterestStorage = new();
    private readonly WorldDatabase _worldDatabase;

    public PointOfInterestCache(WorldDatabase worldDatabase, GridDefines gridDefines)
    {
        _worldDatabase = worldDatabase;
        _gridDefines = gridDefines;
    }

    public PointOfInterest GetPointOfInterest(uint id)
    {
        return _pointsOfInterestStorage.LookupByKey(id);
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        _pointsOfInterestStorage.Clear(); // need for reload case

        //                                   0   1          2          3          4     5      6           7     8
        var result = _worldDatabase.Query("SELECT ID, PositionX, PositionY, PositionZ, Icon, Flags, Importance, Name, WMOGroupID FROM points_of_interest");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 Points of Interest definitions. DB table `points_of_interest` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var id = result.Read<uint>(0);

            PointOfInterest poi = new()
            {
                Id = id,
                Pos = new Vector3(result.Read<float>(1), result.Read<float>(2), result.Read<float>(3)),
                Icon = result.Read<uint>(4),
                Flags = result.Read<uint>(5),
                Importance = result.Read<uint>(6),
                Name = result.Read<string>(7),
                WmoGroupId = result.Read<uint>(8)
            };

            if (!_gridDefines.IsValidMapCoord(poi.Pos.X, poi.Pos.Y, poi.Pos.Z))
            {
                Log.Logger.Error($"Table `points_of_interest` (ID: {id}) have invalid coordinates (PositionX: {poi.Pos.X} PositionY: {poi.Pos.Y} PositionZ: {poi.Pos.Z}), ignored.");

                continue;
            }

            _pointsOfInterestStorage[id] = poi;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} Points of Interest definitions in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }
}