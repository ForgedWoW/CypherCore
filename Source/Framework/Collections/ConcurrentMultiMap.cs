// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Linq;

namespace System.Collections.Generic;

public sealed class ConcurrentMultiMap<TKey, TValue>
{
    private readonly List<TValue> _emptyList = new();

	private readonly Dictionary<TKey, List<TValue>> _interalStorage = new();

	public int Count
	{
		get
		{
			var count = 0;

			lock (_interalStorage)
			foreach (var item in _interalStorage)
				count += item.Value.Count;

			return count;
		}
	}

	public ConcurrentMultiMap() { }

	public void Add(TKey key, TValue value)
	{
        lock (_interalStorage)
            _interalStorage.AddToList(key, value);
	}

	public void AddUnique(TKey key, TValue value)
	{
        lock (_interalStorage)
            _interalStorage.AddUniqueToList(key, value);
	}

	public void AddRange(TKey key, IEnumerable<TValue> valueList)
	{
		lock (_interalStorage)
		{
			if (!_interalStorage.TryGetValue(key, out var val))
			{
				val = new List<TValue>();
				_interalStorage.Add(key, val);
			}

			val.AddRange(valueList);
		}
	}

	public bool Remove(TKey key)
	{
        lock (_interalStorage)
            return _interalStorage.Remove(key);
	}

	public bool Remove(TKey key, TValue value)
	{
        lock (_interalStorage)
            return _interalStorage.Remove(key, value);
	}

	public bool ContainsKey(TKey key)
	{
        lock (_interalStorage)
            return _interalStorage.ContainsKey(key);
	}

	public bool Contains(TKey key, TValue item)
	{
		lock (_interalStorage)
		{
			if (!_interalStorage.ContainsKey(key)) return false;

			return _interalStorage[key].Contains(item);
		}
	}

	public List<TValue> LookupByKey(TKey key)
	{
        lock (_interalStorage)
            if (_interalStorage.TryGetValue(key, out var values))
				return values;

		return _emptyList;
	}

	public bool TryGetValue(TKey key, out List<TValue> value)
	{
        lock (_interalStorage)
            return _interalStorage.TryGetValue(key, out value);
	}

	public void Clear()
	{
        lock (_interalStorage)
            _interalStorage.Clear();
	}

	public bool Empty()
	{
        lock (_interalStorage)
            return _interalStorage == null || _interalStorage.Count == 0;
	}
}