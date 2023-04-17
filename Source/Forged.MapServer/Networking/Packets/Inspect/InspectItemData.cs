// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Inspect;

public class InspectItemData
{
    public List<AzeriteEssenceData> AzeriteEssences = new();
    public List<int> AzeritePowers = new();
    public ObjectGuid CreatorGUID;
    public List<InspectEnchantData> Enchants = new();
    public List<ItemGemData> Gems = new();
    public byte Index;
    public ItemInstance Item;
    public bool Usable;

    public InspectItemData(Entities.Items.Item item, byte index)
    {
        CreatorGUID = item.Creator;

        Item = new ItemInstance(item);
        Index = index;
        Usable = true; // @todo

        for (EnchantmentSlot enchant = 0; enchant < EnchantmentSlot.Max; ++enchant)
        {
            var enchId = item.GetEnchantmentId(enchant);

            if (enchId != 0)
                Enchants.Add(new InspectEnchantData(enchId, (byte)enchant));
        }

        byte i = 0;

        foreach (var gemData in item.ItemData.Gems)
        {
            if (gemData.ItemId != 0)
            {
                ItemGemData gem = new()
                {
                    Slot = i,
                    Item = new ItemInstance(gemData)
                };

                Gems.Add(gem);
            }

            ++i;
        }

        var azeriteItem = item.AsAzeriteItem;

        var essences = azeriteItem?.GetSelectedAzeriteEssences();

        if (essences != null)
            for (byte slot = 0; slot < essences.AzeriteEssenceID.GetSize(); ++slot)
            {
                AzeriteEssenceData essence = new()
                {
                    Index = slot,
                    AzeriteEssenceID = essences.AzeriteEssenceID[slot]
                };

                if (essence.AzeriteEssenceID != 0)
                {
                    essence.Rank = azeriteItem.GetEssenceRank(essence.AzeriteEssenceID);
                    essence.SlotUnlocked = true;
                }
                else
                    essence.SlotUnlocked = azeriteItem.HasUnlockedEssenceSlot(slot);

                AzeriteEssences.Add(essence);
            }
    }

    public void Write(WorldPacket data)
    {
        data.WritePackedGuid(CreatorGUID);
        data.WriteUInt8(Index);
        data.WriteInt32(AzeritePowers.Count);
        data.WriteInt32(AzeriteEssences.Count);

        foreach (var id in AzeritePowers)
            data.WriteInt32(id);

        Item.Write(data);
        data.WriteBit(Usable);
        data.WriteBits(Enchants.Count, 4);
        data.WriteBits(Gems.Count, 2);
        data.FlushBits();

        foreach (var azeriteEssenceData in AzeriteEssences)
            azeriteEssenceData.Write(data);

        foreach (var enchantData in Enchants)
            enchantData.Write(data);

        foreach (var gem in Gems)
            gem.Write(data);
    }
}