// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.Quest;

public sealed class PlayerChoiceResponseRewardEntry
{
	public ItemInstance Item;
	public int Quantity;

	public void Write(WorldPacket data)
	{
		Item = new ItemInstance();
		Item.Write(data);
		data.WriteInt32(Quantity);
	}
}