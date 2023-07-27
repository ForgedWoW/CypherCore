// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class ExplorationExpCache : IObjectCache
{
    private readonly WorldDatabase _worldDatabase;
    private readonly Dictionary<uint, uint> _baseXPTable = new();

    public ExplorationExpCache(WorldDatabase worldDatabase)
    {
        _worldDatabase = worldDatabase;
    }

    public uint GetBaseXP(uint level)
    {
        return _baseXPTable.ContainsKey(level) ? _baseXPTable[level] : 0;
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        var result = _worldDatabase.Query("SELECT level, basexp FROM exploration_basexp");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 BaseXP definitions. DB table `exploration_basexp` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var level = result.Read<byte>(0);
            var basexp = result.Read<uint>(1);
            _baseXPTable[level] = basexp;
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} BaseXP definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}