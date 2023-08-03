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

public class PointOfInterestLocaleCache : IObjectCache
{
    private readonly WorldDatabase _worldDatabase;
    private readonly Dictionary<uint, PointOfInterestLocale> _pointOfInterestLocaleStorage = new();

    public PointOfInterestLocaleCache(WorldDatabase worldDatabase)
    {
        _worldDatabase = worldDatabase;
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        _pointOfInterestLocaleStorage.Clear(); // need for reload case

        //                                        0      1      2
        var result = _worldDatabase.Query("SELECT ID, locale, Name FROM points_of_interest_locale");

        if (result.IsEmpty())
            return;

        do
        {
            var id = result.Read<uint>(0);
            var localeName = result.Read<string>(1);
            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                continue;

            if (!_pointOfInterestLocaleStorage.ContainsKey(id))
                _pointOfInterestLocaleStorage[id] = new PointOfInterestLocale();

            var data = _pointOfInterestLocaleStorage[id];
            AddLocaleString(result.Read<string>(2), locale, data.Name);
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} points_of_interest locale strings in {1} ms", _pointOfInterestLocaleStorage.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public PointOfInterestLocale GetPointOfInterestLocale(uint id)
    {
        return _pointOfInterestLocaleStorage.LookupByKey(id);
    }

    private void AddLocaleString(string value, Locale locale, StringArray data)
    {
        if (!string.IsNullOrEmpty(value))
            data[(int)locale] = value;
    }
}