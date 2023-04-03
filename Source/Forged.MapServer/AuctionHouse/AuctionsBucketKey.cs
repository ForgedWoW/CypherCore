// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Networking.Packets.AuctionHouse;
using Framework.Constants;

namespace Forged.MapServer.AuctionHouse;

public class AuctionsBucketKey : IComparable<AuctionsBucketKey>
{
    public AuctionsBucketKey(uint itemId, ushort itemLevel, ushort battlePetSpeciesId, ushort suffixItemNameDescriptionId)
    {
        ItemId = itemId;
        ItemLevel = itemLevel;
        BattlePetSpeciesId = battlePetSpeciesId;
        SuffixItemNameDescriptionId = suffixItemNameDescriptionId;
    }

    public AuctionsBucketKey(AuctionBucketKey key)
    {
        ItemId = key.ItemID;
        ItemLevel = key.ItemLevel;
        BattlePetSpeciesId = key.BattlePetSpeciesID ?? 0;
        SuffixItemNameDescriptionId = key.SuffixItemNameDescriptionID ?? 0;
    }

    public ushort BattlePetSpeciesId { get; }
    public uint ItemId { get; }
    public ushort ItemLevel { get; }
    public ushort SuffixItemNameDescriptionId { get; }

    public static AuctionsBucketKey ForCommodity(ItemTemplate itemTemplate)
    {
        return new AuctionsBucketKey(itemTemplate.Id, (ushort)itemTemplate.BaseItemLevel, 0, 0);
    }

    public static AuctionsBucketKey ForItem(Item item)
    {
        var itemTemplate = item.Template;

        if (itemTemplate.MaxStackSize == 1)
            return new AuctionsBucketKey(item.Entry,
                                         (ushort)Item.GetItemLevel(itemTemplate, item.BonusData, 0, (uint)item.GetRequiredLevel(), 0, 0, 0, false, 0),
                                         (ushort)item.GetModifier(ItemModifier.BattlePetSpeciesId),
                                         (ushort)item.BonusData.Suffix);
        else
            return ForCommodity(itemTemplate);
    }

    public static bool operator !=(AuctionsBucketKey right, AuctionsBucketKey left)
    {
        return !(right == left);
    }

    public static bool operator ==(AuctionsBucketKey right, AuctionsBucketKey left)
    {
        return left != null && right != null && right.ItemId == left.ItemId && right.ItemLevel == left.ItemLevel && right.BattlePetSpeciesId == left.BattlePetSpeciesId && right.SuffixItemNameDescriptionId == left.SuffixItemNameDescriptionId;
    }

    public int CompareTo(AuctionsBucketKey other)
    {
        return ItemId.CompareTo(other.ItemId);
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return ItemId.GetHashCode() ^ ItemLevel.GetHashCode() ^ BattlePetSpeciesId.GetHashCode() ^ SuffixItemNameDescriptionId.GetHashCode();
    }
}