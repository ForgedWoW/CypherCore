// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Text;
using Framework.Constants;

namespace Forged.MapServer.Maps.Workers;

public class LocalizedDo : IDoWork<Player>
{
    private readonly MessageBuilder _localizer;
    private IDoWork<Player>[] _localizedCache = new IDoWork<Player>[(int)Locale.Total]; // 0 = default, i => i-1 locale index

    public LocalizedDo(MessageBuilder localizer)
    {
        _localizer = localizer;
    }

    public void Invoke(Player player)
    {
        var locIdx = player.Session.SessionDbLocaleIndex;
        var cacheIdx = (int)locIdx + 1;
        IDoWork<Player> action;

        // create if not cached yet
        if (_localizedCache.Length < cacheIdx + 1 || _localizedCache[cacheIdx] == null)
        {
            if (_localizedCache.Length < cacheIdx + 1)
                Array.Resize(ref _localizedCache, cacheIdx + 1);

            action = _localizer.Invoke(locIdx);
            _localizedCache[cacheIdx] = action;
        }
        else
        {
            action = _localizedCache[cacheIdx];
        }

        action.Invoke(player);
    }
}