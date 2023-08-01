// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class CreatureLocaleCache : IObjectCache
{
    private readonly Dictionary<uint, CreatureLocale> _creatureLocaleStorage = new();
    private readonly WorldDatabase _worldDatabase;

    public CreatureLocaleCache(WorldDatabase worldDatabase)
    {
        _worldDatabase = worldDatabase;
    }

    public CreatureLocale GetCreatureLocale(uint entry)
    {
        return _creatureLocaleStorage.LookupByKey(entry);
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        _creatureLocaleStorage.Clear(); // need for reload case

        //                                         0      1       2     3        4      5
        var result = _worldDatabase.Query("SELECT entry, locale, Name, NameAlt, Title, TitleAlt FROM creature_template_locale");

        if (result.IsEmpty())
            return;

        do
        {
            var id = result.Read<uint>(0);
            var localeName = result.Read<string>(1);
            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                continue;

            if (!_creatureLocaleStorage.ContainsKey(id))
                _creatureLocaleStorage[id] = new CreatureLocale();

            var data = _creatureLocaleStorage[id];
            AddLocaleString(result.Read<string>(2), locale, data.Name);
            AddLocaleString(result.Read<string>(3), locale, data.NameAlt);
            AddLocaleString(result.Read<string>(4), locale, data.Title);
            AddLocaleString(result.Read<string>(5), locale, data.TitleAlt);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} creature locale strings in {1} ms", _creatureLocaleStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    private void AddLocaleString(string value, Locale locale, StringArray data)
    {
        if (!string.IsNullOrEmpty(value))
            data[(int)locale] = value;
    }
}