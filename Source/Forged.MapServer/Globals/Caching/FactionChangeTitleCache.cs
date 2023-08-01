// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.C;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class FactionChangeTitleCache : IObjectCache
{
    private readonly DB6Storage<CharTitlesRecord> _charTitlesRecords;
    private readonly WorldDatabase _worldDatabase;

    public FactionChangeTitleCache(WorldDatabase worldDatabase, DB6Storage<CharTitlesRecord> charTitlesRecords)
    {
        _worldDatabase = worldDatabase;
        _charTitlesRecords = charTitlesRecords;
    }

    public Dictionary<uint, uint> FactionChangeTitles { get; set; } = new();

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT alliance_id, horde_id FROM player_factionchange_titles");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 faction change title pairs. DB table `player_factionchange_title` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var alliance = result.Read<uint>(0);
            var horde = result.Read<uint>(1);

            if (!_charTitlesRecords.ContainsKey(alliance))
                Log.Logger.Error("Title {0} (alliance_id) referenced in `player_factionchange_title` does not exist, pair skipped!", alliance);
            else if (!_charTitlesRecords.ContainsKey(horde))
                Log.Logger.Error("Title {0} (horde_id) referenced in `player_factionchange_title` does not exist, pair skipped!", horde);
            else
                FactionChangeTitles[alliance] = horde;

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} faction change title pairs in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}