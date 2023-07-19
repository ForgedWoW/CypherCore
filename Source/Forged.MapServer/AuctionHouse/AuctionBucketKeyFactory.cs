// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Items;
using Framework.Constants;

namespace Forged.MapServer.AuctionHouse;

public class AuctionBucketKeyFactory
{
    private readonly ItemFactory _itemFactory;

    public AuctionBucketKeyFactory(ItemFactory itemFactory)
    {
        _itemFactory = itemFactory;
    }

    public AuctionsBucketKey ForCommodity(ItemTemplate itemTemplate)
    {
        return new AuctionsBucketKey(itemTemplate.Id, (ushort)itemTemplate.BaseItemLevel, 0, 0);
    }

    public AuctionsBucketKey ForItem(Item item)
    {
        var itemTemplate = item.Template;

        if (itemTemplate.MaxStackSize == 1)
            return new AuctionsBucketKey(item.Entry,
                                         (ushort)_itemFactory.GetItemLevel(itemTemplate, item.BonusData, 0, (uint)item.GetRequiredLevel(), 0, 0, 0, false, 0),
                                         (ushort)item.GetModifier(ItemModifier.BattlePetSpeciesId),
                                         (ushort)item.BonusData.Suffix);

        return ForCommodity(itemTemplate);
    }
}