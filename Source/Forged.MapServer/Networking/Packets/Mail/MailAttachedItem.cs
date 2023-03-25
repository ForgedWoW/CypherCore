// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Mail;

public class MailAttachedItem
{
	public byte Position;
	public ulong AttachID;
	public ItemInstance Item;
	public uint Count;
	public int Charges;
	public uint MaxDurability;
	public uint Durability;
	public bool Unlocked;
	readonly List<ItemEnchantData> Enchants = new();
	readonly List<ItemGemData> Gems = new();

	public MailAttachedItem(Entities.Items.Item item, byte pos)
	{
		Position = pos;
		AttachID = item.GUID.Counter;
		Item = new ItemInstance(item);
		Count = item.Count;
		Charges = item.GetSpellCharges();
		MaxDurability = item.ItemData.MaxDurability;
		Durability = item.ItemData.Durability;
		Unlocked = !item.IsLocked;

		for (EnchantmentSlot slot = 0; slot < EnchantmentSlot.MaxInspected; slot++)
		{
			if (item.GetEnchantmentId(slot) == 0)
				continue;

			Enchants.Add(new ItemEnchantData(item.GetEnchantmentId(slot), item.GetEnchantmentDuration(slot), (int)item.GetEnchantmentCharges(slot), (byte)slot));
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
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt8(Position);
		data.WriteUInt64(AttachID);
		data.WriteUInt32(Count);
		data.WriteInt32(Charges);
		data.WriteUInt32(MaxDurability);
		data.WriteUInt32(Durability);
		Item.Write(data);
		data.WriteBits(Enchants.Count, 4);
		data.WriteBits(Gems.Count, 2);
		data.WriteBit(Unlocked);
		data.FlushBits();

		foreach (var gem in Gems)
			gem.Write(data);

		foreach (var en in Enchants)
			en.Write(data);
	}
}