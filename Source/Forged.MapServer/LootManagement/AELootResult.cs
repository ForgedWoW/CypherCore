using System.Collections.Generic;
using Forged.MapServer.Entities.Items;
using Framework.Constants;
using Forged.MapServer.LootManagement;

namespace Forged.MapServer.LootManagement;

public class AELootResult
{
    private readonly List<ResultValue> _byOrder = new();
    private readonly Dictionary<Item, int> _byItem = new();

    public void Add(Item item, byte count, LootType lootType, uint dungeonEncounterId)
    {
        var id = _byItem.LookupByKey(item);

        if (id != 0)
        {
            var resultValue = _byOrder[id];
            resultValue.Count += count;
        }
        else
        {
            _byItem[item] = _byOrder.Count;
            ResultValue value;
            value.Item = item;
            value.Count = count;
            value.LootType = lootType;
            value.DungeonEncounterId = dungeonEncounterId;
            _byOrder.Add(value);
        }
    }

    public List<ResultValue> GetByOrder()
    {
        return _byOrder;
    }

    public struct ResultValue
    {
        public Item Item;
        public byte Count;
        public LootType LootType;
        public uint DungeonEncounterId;
    }
}
