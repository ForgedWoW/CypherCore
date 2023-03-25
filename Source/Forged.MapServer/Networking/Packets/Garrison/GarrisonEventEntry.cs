// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

struct GarrisonEventEntry
{
	public int EntryID;
	public long EventValue;

	public void Write(WorldPacket data)
	{
		data.WriteInt64(EventValue);
		data.WriteInt32(EntryID);
	}
}