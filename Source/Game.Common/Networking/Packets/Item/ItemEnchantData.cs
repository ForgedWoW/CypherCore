// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Item;

public class ItemEnchantData
{
	public uint ID;
	public uint Expiration;
	public int Charges;
	public byte Slot;
	public ItemEnchantData() { }

	public ItemEnchantData(uint id, uint expiration, int charges, byte slot)
	{
		ID = id;
		Expiration = expiration;
		Charges = charges;
		Slot = slot;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(ID);
		data.WriteUInt32(Expiration);
		data.WriteInt32(Charges);
		data.WriteUInt8(Slot);
	}
}
