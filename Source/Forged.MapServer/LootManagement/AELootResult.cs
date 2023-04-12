// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Items;
using Framework.Constants;

namespace Forged.MapServer.LootManagement;

public class AELootResult
{
    private readonly Dictionary<Item, int> _byItem = new();

    public List<ResultValue> ByOrder { get; } = new();

    public void Add(Item item, byte count, LootType lootType, uint dungeonEncounterId)
    {
        if (_byItem.TryGetValue(item, out var id))
        {
            var resultValue = ByOrder[id];
            resultValue.Count += count;
        }
        else
        {
            _byItem[item] = ByOrder.Count;
            ResultValue value;
            value.Item = item;
            value.Count = count;
            value.LootType = lootType;
            value.DungeonEncounterId = dungeonEncounterId;
            ByOrder.Add(value);
        }
    }

    public struct ResultValue
    {
        public byte Count;
        public uint DungeonEncounterId;
        public Item Item;
        public LootType LootType;
    }
}