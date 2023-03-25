// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public struct SpellCraftingReagent
{
	public int ItemID;
	public int DataSlotIndex;
	public int Quantity;
	public byte? Unknown_1000;

	public void Read(WorldPacket data)
	{
		ItemID = data.ReadInt32();
		DataSlotIndex = data.ReadInt32();
		Quantity = data.ReadInt32();

		if (data.HasBit())
			Unknown_1000 = data.ReadUInt8();
	}
}