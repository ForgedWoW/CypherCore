// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer;

public class AuctionsBucketKey : IComparable<AuctionsBucketKey>
{
	public uint ItemId { get; set; }
	public ushort ItemLevel { get; set; }
	public ushort BattlePetSpeciesId { get; set; }
	public ushort SuffixItemNameDescriptionId { get; set; }

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
		BattlePetSpeciesId = (ushort)(key.BattlePetSpeciesID.HasValue ? key.BattlePetSpeciesID.Value : 0);
		SuffixItemNameDescriptionId = (ushort)(key.SuffixItemNameDescriptionID.HasValue ? key.SuffixItemNameDescriptionID.Value : 0);
	}

	public int CompareTo(AuctionsBucketKey other)
	{
		return ItemId.CompareTo(other.ItemId);
	}

	public static bool operator ==(AuctionsBucketKey right, AuctionsBucketKey left)
	{
		return right.ItemId == left.ItemId && right.ItemLevel == left.ItemLevel && right.BattlePetSpeciesId == left.BattlePetSpeciesId && right.SuffixItemNameDescriptionId == left.SuffixItemNameDescriptionId;
	}

	public static bool operator !=(AuctionsBucketKey right, AuctionsBucketKey left)
	{
		return !(right == left);
	}

	public override bool Equals(object obj)
	{
		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return ItemId.GetHashCode() ^ ItemLevel.GetHashCode() ^ BattlePetSpeciesId.GetHashCode() ^ SuffixItemNameDescriptionId.GetHashCode();
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

	public static AuctionsBucketKey ForCommodity(ItemTemplate itemTemplate)
	{
		return new AuctionsBucketKey(itemTemplate.Id, (ushort)itemTemplate.BaseItemLevel, 0, 0);
	}
}