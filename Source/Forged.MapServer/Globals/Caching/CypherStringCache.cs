// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class CypherStringCache : IObjectCache
{
    private readonly Dictionary<uint, StringArray> _cypherStringStorage = new();
    private readonly WorldDatabase _worldDatabase;

    public CypherStringCache(WorldDatabase worldDatabase)
    {
        _worldDatabase = worldDatabase;
    }

    public void Load()
    {
        var time = Time.MSTime;
        _cypherStringStorage.Clear();

        var result = _worldDatabase.Query("SELECT entry, content_default, content_loc1, content_loc2, content_loc3, content_loc4, content_loc5, content_loc6, content_loc7, content_loc8 FROM trinity_string");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 CypherStrings. DB table `trinity_string` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);

            _cypherStringStorage[entry] = new StringArray((int)SharedConst.DefaultLocale + 1);
            count++;

            for (var i = SharedConst.DefaultLocale; i >= 0; --i)
                AddLocaleString(result.Read<string>((int)i + 1).ConvertFormatSyntax(), i, _cypherStringStorage[entry]);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} CypherStrings in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }

    public string GetCypherString(uint entry, Locale locale = Locale.enUS)
    {
        if (!_cypherStringStorage.ContainsKey(entry))
        {
            Log.Logger.Error("Cypher string entry {0} not found in DB.", entry);

            return "<Error>";
        }

        var cs = _cypherStringStorage[entry];

        if (cs.Length > (int)locale && !string.IsNullOrEmpty(cs[(int)locale]))
            return cs[(int)locale];

        return cs[(int)SharedConst.DefaultLocale];
    }

    public string GetCypherString(CypherStrings cmd, Locale locale = Locale.enUS)
    {
        return GetCypherString((uint)cmd, locale);
    }

    private void AddLocaleString(string value, Locale locale, StringArray data)
    {
        if (!string.IsNullOrEmpty(value))
            data[(int)locale] = value;
    }
}