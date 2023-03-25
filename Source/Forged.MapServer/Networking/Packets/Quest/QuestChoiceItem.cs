// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

public struct QuestChoiceItem
{
	public LootItemType LootItemType;
	public ItemInstance Item;
	public uint Quantity;

	public void Read(WorldPacket data)
	{
		data.ResetBitPos();
		LootItemType = (LootItemType)data.ReadBits<byte>(2);
		Item = new ItemInstance();
		Item.Read(data);
		Quantity = data.ReadUInt32();
	}

	public void Write(WorldPacket data)
	{
		data.WriteBits((byte)LootItemType, 2);
		Item.Write(data);
		data.WriteUInt32(Quantity);
	}
}