// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.LFG;

namespace Game.Common.Networking.Packets.LFG;

public class LFGBlackListPkt
{
	public ObjectGuid? PlayerGuid;
	public List<LFGBlackListSlot> Slot = new();

	public void Write(WorldPacket data)
	{
		data.WriteBit(PlayerGuid.HasValue);
		data.WriteInt32(Slot.Count);

		if (PlayerGuid.HasValue)
			data.WritePackedGuid(PlayerGuid.Value);

		foreach (var slot in Slot)
			slot.Write(data);
	}
}
