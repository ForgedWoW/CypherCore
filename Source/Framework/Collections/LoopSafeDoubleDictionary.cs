// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Framework.Collections;

public class LoopSafeDoubleDictionary<TKey1, TKey2, TVal> : Dictionary<TKey1, Dictionary<TKey2, TVal>>
{
    private readonly Dictionary<TKey1, List<TKey2>> _removeCache = new();

    public void ExecuteRemove()
    {
        foreach (var kvp in _removeCache)
            foreach (var val in kvp.Value)
                this.Remove(kvp.Key, val);

        _removeCache.Clear();
    }

    public void QueueRemove(TKey1 key1, TKey2 key2)
    {
        _removeCache.Add(key1, key2);
    }
}