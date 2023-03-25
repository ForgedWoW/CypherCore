// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Entities.Creatures;

public class VendorItemData
{
	readonly List<VendorItem> _items = new();

	public VendorItem GetItem(uint slot)
	{
		if (slot >= _items.Count)
			return null;

		return _items[(int)slot];
	}

	public bool Empty()
	{
		return _items.Count == 0;
	}

	public int GetItemCount()
	{
		return _items.Count;
	}

	public void AddItem(VendorItem vItem)
	{
		_items.Add(vItem);
	}

	public bool RemoveItem(uint item_id, ItemVendorType type)
	{
		var i = _items.RemoveAll(p => p.Item == item_id && p.Type == type);

		if (i == 0)
			return false;
		else
			return true;
	}

	public VendorItem FindItemCostPair(uint itemId, uint extendedCost, ItemVendorType type)
	{
		return _items.Find(p => p.Item == itemId && p.ExtendedCost == extendedCost && p.Type == type);
	}

	public void Clear()
	{
		_items.Clear();
	}
}