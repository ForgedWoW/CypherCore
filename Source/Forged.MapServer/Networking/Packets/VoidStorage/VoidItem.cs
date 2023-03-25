﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

struct VoidItem
{
	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(Guid);
		data.WritePackedGuid(Creator);
		data.WriteUInt32(Slot);
		Item.Write(data);
	}

	public ObjectGuid Guid;
	public ObjectGuid Creator;
	public uint Slot;
	public ItemInstance Item;
}