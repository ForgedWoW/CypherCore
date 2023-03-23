// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.Item;

namespace Game.Common.Networking.Packets.Loot;

public class LootItemData
{
	public byte Type;
	public LootSlotType UIType;
	public uint Quantity;
	public byte LootItemType;
	public byte LootListID;
	public bool CanTradeToTapList;
	public ItemInstance Loot;

	public void Write(WorldPacket data)
	{
		data.WriteBits(Type, 2);
		data.WriteBits(UIType, 3);
		data.WriteBit(CanTradeToTapList);
		data.FlushBits();
		Loot.Write(data); // WorldPackets::Item::ItemInstance
		data.WriteUInt32(Quantity);
		data.WriteUInt8(LootItemType);
		data.WriteUInt8(LootListID);
	}
}
