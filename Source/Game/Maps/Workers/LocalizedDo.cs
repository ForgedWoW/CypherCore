using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Chat;
using Game.Entities;

namespace Game.Maps;

public class LocalizedDo : IDoWork<Player>
{
    public LocalizedDo(MessageBuilder localizer)
    {
        _localizer = localizer;
    }

    public void Invoke(Player player)
    {
        Locale loc_idx = player.GetSession().GetSessionDbLocaleIndex();
        int cache_idx = (int)loc_idx + 1;
        IDoWork<Player> action;

        // create if not cached yet
        if (_localizedCache.Length < cache_idx + 1 || _localizedCache[cache_idx] == null)
        {
            if (_localizedCache.Length < cache_idx + 1)
                Array.Resize(ref _localizedCache, cache_idx + 1);

            action = _localizer.Invoke(loc_idx);
            _localizedCache[cache_idx] = action;
        }
        else
            action = _localizedCache[cache_idx];

        action.Invoke(player);
    }

    readonly MessageBuilder _localizer;
    IDoWork<Player>[] _localizedCache = new IDoWork<Player>[(int)Locale.Total];     // 0 = default, i => i-1 locale index
}