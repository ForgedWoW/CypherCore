// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.AuctionHouse;

class AuctionsResultBuilder<T>
{
	readonly uint _offset;
	readonly IComparer<T> _sorter;
	readonly AuctionHouseResultLimits _maxResults;
	readonly List<T> _items = new();
	bool _hasMoreResults;

	public AuctionsResultBuilder(uint offset, IComparer<T> sorter, AuctionHouseResultLimits maxResults)
	{
		_offset = offset;
		_sorter = sorter;
		_maxResults = maxResults;
		_hasMoreResults = false;
	}

	public void AddItem(T item)
	{
		var index = _items.BinarySearch(item, _sorter);

		if (index < 0)
			index = ~index;

		_items.Insert(index, item);

		if (_items.Count > (int)_maxResults + _offset)
		{
			_items.RemoveAt(_items.Count - 1);
			_hasMoreResults = true;
		}
	}

	public Span<T> GetResultRange()
	{
		Span<T> h = _items.ToArray();

		return h[(int)_offset..];
	}

	public bool HasMoreResults()
	{
		return _hasMoreResults;
	}
}