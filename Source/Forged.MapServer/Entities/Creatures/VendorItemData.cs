// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Entities.Creatures;

public class VendorItemData
{
    private readonly List<VendorItem> _items = new();

    public bool Empty => _items.Count == 0;

    public int ItemCount => _items.Count;

    public void AddItem(VendorItem vItem)
    {
        _items.Add(vItem);
    }

    public void Clear()
    {
        _items.Clear();
    }

    public VendorItem FindItemCostPair(uint itemId, uint extendedCost, ItemVendorType type)
    {
        return _items.Find(p => p.Item == itemId && p.ExtendedCost == extendedCost && p.Type == type);
    }

    public VendorItem GetItem(uint slot)
    {
        return slot >= _items.Count ? null : _items[(int)slot];
    }

    public bool RemoveItem(uint itemID, ItemVendorType type)
    {
        var i = _items.RemoveAll(p => p.Item == itemID && p.Type == type);

        return i != 0;
    }
}