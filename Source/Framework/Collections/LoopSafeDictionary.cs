// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Framework.Collections;

public class LoopSafeDictionary<TKey, TVal> : Dictionary<TKey, TVal>
{
    private readonly List<TKey> _removeCache = new();

    public void ExecuteRemove()
    {
        foreach (var kvp in _removeCache)
            Remove(kvp);

        _removeCache.Clear();
    }

    public void QueueRemove(TKey key)
    {
        _removeCache.Add(key);
    }
}