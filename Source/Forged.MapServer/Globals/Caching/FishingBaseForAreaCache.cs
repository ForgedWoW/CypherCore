// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class FishingBaseForAreaCache : IObjectCache
{
    private readonly DB6Storage<AreaTableRecord> _areaTableRecords;
    private readonly Dictionary<uint, int> _fishingBaseForAreaStorage = new();
    private readonly WorldDatabase _worldDatabase;

    public FishingBaseForAreaCache(WorldDatabase worldDatabase, DB6Storage<AreaTableRecord> areaTableRecords)
    {
        _worldDatabase = worldDatabase;
        _areaTableRecords = areaTableRecords;
    }

    public int GetFishingBaseSkillLevel(uint entry)
    {
        return _fishingBaseForAreaStorage.LookupByKey(entry);
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        _fishingBaseForAreaStorage.Clear(); // for reload case

        var result = _worldDatabase.Query("SELECT entry, skill FROM skill_fishing_base_level");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 areas for fishing base skill level. DB table `skill_fishing_base_level` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);
            var skill = result.Read<int>(1);

            if (!_areaTableRecords.TryGetValue(entry, out _))
            {
                Log.Logger.Error("AreaId {0} defined in `skill_fishing_base_level` does not exist", entry);

                continue;
            }

            _fishingBaseForAreaStorage[entry] = skill;
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} areas for fishing base skill level in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}