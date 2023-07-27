// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class PageTextCache : IObjectCache
{
    private readonly Dictionary<uint, PageText> _pageTextStorage = new();
    private readonly WorldDatabase _worldDatabase;

    public PageTextCache(WorldDatabase worldDatabase)
    {
        _worldDatabase = worldDatabase;
    }

    public PageText GetPageText(uint pageEntry)
    {
        return _pageTextStorage.LookupByKey(pageEntry);
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        //                                         0   1     2           3                 4
        var result = _worldDatabase.Query("SELECT ID, `text`, NextPageID, PlayerConditionID, Flags FROM page_text");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 page texts. DB table `page_text` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var id = result.Read<uint>(0);

            PageText pageText = new()
            {
                Text = result.Read<string>(1),
                NextPageID = result.Read<uint>(2),
                PlayerConditionID = result.Read<int>(3),
                Flags = result.Read<byte>(4)
            };

            _pageTextStorage[id] = pageText;
            ++count;
        } while (result.NextRow());

        foreach (var pair in _pageTextStorage)
            if (pair.Value.NextPageID != 0)
                if (!_pageTextStorage.ContainsKey(pair.Value.NextPageID))
                    Log.Logger.Error("Page text (ID: {0}) has non-existing `NextPageID` ({1})", pair.Key, pair.Value.NextPageID);

        Log.Logger.Information("Loaded {0} page texts in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}