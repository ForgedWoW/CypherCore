// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Garrison;

class GarrisonCollection
{
	public int Type;
	public List<GarrisonCollectionEntry> Entries = new();

	public void Write(WorldPacket data)
	{
		data.WriteInt32(Type);
		data.WriteInt32(Entries.Count);

		foreach (var collectionEntry in Entries)
			collectionEntry.Write(data);
	}
}