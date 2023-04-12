// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.LootManagement;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

public class ItemInstance
{
    public ItemBonuses ItemBonus;
    public uint ItemID;
    public ItemModList Modifications = new();

    public ItemInstance() { }

    public ItemInstance(Entities.Items.Item item)
    {
        ItemID = item.Entry;
        var bonusListIds = item.GetBonusListIDs();

        if (!bonusListIds.Empty())
        {
            ItemBonus = new ItemBonuses();
            ItemBonus.BonusListIDs.AddRange(bonusListIds);
            ItemBonus.Context = item.GetContext();
        }

        foreach (var mod in item.ItemData.Modifiers.Value.Values)
            Modifications.Values.Add(new ItemMod(mod.Value, (ItemModifier)mod.Type));
    }

    public ItemInstance(LootItem lootItem)
    {
        ItemID = lootItem.Itemid;

        if (!lootItem.BonusListIDs.Empty() || lootItem.RandomBonusListId != 0)
        {
            ItemBonus = new ItemBonuses
            {
                BonusListIDs = lootItem.BonusListIDs,
                Context = lootItem.Context
            };

            if (lootItem.RandomBonusListId != 0)
                ItemBonus.BonusListIDs.Add(lootItem.RandomBonusListId);
        }
    }

    public ItemInstance(VoidStorageItem voidItem)
    {
        ItemID = voidItem.ItemEntry;

        if (voidItem.FixedScalingLevel != 0)
            Modifications.Values.Add(new ItemMod(voidItem.FixedScalingLevel, ItemModifier.TimewalkerLevel));

        if (voidItem.ArtifactKnowledgeLevel != 0)
            Modifications.Values.Add(new ItemMod(voidItem.ArtifactKnowledgeLevel, ItemModifier.ArtifactKnowledgeLevel));

        if (!voidItem.BonusListIDs.Empty())
            ItemBonus = new ItemBonuses
            {
                Context = voidItem.Context,
                BonusListIDs = voidItem.BonusListIDs
            };
    }

    public ItemInstance(SocketedGem gem)
    {
        ItemID = gem.ItemId;

        ItemBonuses bonus = new()
        {
            Context = (ItemContext)(byte)gem.Context
        };

        foreach (var bonusListId in gem.BonusListIDs)
            if (bonusListId != 0)
                bonus.BonusListIDs.Add(bonusListId);

        if (bonus.Context != 0 || !bonus.BonusListIDs.Empty())
            ItemBonus = bonus;
    }

    public static bool operator !=(ItemInstance left, ItemInstance right)
    {
        return !(left == right);
    }

    public static bool operator ==(ItemInstance left, ItemInstance right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (ReferenceEquals(left, null))
            return false;

        if (ReferenceEquals(right, null))
            return false;

        if (left.ItemID != right.ItemID)
            return false;

        if (left.ItemBonus != null && right.ItemBonus != null && left.ItemBonus != right.ItemBonus)
            return false;

        if (left.Modifications != right.Modifications)
            return false;

        return true;
    }

    public override bool Equals(object obj)
    {
        if (obj is ItemInstance instance)
            return instance == this;

        return false;
    }

    public override int GetHashCode()
    {
        return ItemID.GetHashCode() ^ ItemBonus.GetHashCode() ^ Modifications.GetHashCode();
    }

    public void Read(WorldPacket data)
    {
        ItemID = data.ReadUInt32();

        if (data.HasBit())
            ItemBonus = new ItemBonuses();

        data.ResetBitPos();

        Modifications.Read(data);

        if (ItemBonus != null)
            ItemBonus.Read(data);
    }

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(ItemID);

        data.WriteBit(ItemBonus != null);
        data.FlushBits();

        Modifications.Write(data);

        if (ItemBonus != null)
            ItemBonus.Write(data);
    }
}