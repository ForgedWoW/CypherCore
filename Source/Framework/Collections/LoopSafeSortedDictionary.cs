// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Framework.Collections;

public class LoopSafeSortedDictionary<TKey, TVal> : SortedDictionary<TKey, TVal>
{
	private readonly List<TKey> _removeCache = new();

	public void QueueRemove(TKey key)
	{
		_removeCache.Add(key);
	}

	public void ExecuteRemove()
	{
		foreach (var kvp in _removeCache)
			this.Remove(kvp);

		_removeCache.Clear();
	}
}