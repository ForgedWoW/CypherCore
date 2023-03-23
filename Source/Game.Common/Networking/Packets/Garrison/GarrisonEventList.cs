﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.Garrison;

namespace Game.Common.Networking.Packets.Garrison;

public class GarrisonEventList
{
	public int Type;
	public List<GarrisonEventEntry> Events = new();

	public void Write(WorldPacket data)
	{
		data.WriteInt32(Type);
		data.WriteInt32(Events.Count);

		foreach (var eventEntry in Events)
			eventEntry.Write(data);
	}
}
