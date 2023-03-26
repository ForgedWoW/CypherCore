// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Garrison;

internal struct GarrisonCollectionEntry
{
	public int EntryID;
	public int Rank;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(EntryID);
		data.WriteInt32(Rank);
	}
}