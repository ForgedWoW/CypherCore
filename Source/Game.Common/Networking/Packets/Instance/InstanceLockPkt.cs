// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Instance;

public struct InstanceLockPkt
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt32(MapID);
		data.WriteUInt32(DifficultyID);
		data.WriteUInt64(InstanceID);
		data.WriteInt32(TimeRemaining);
		data.WriteUInt32(CompletedMask);

		data.WriteBit(Locked);
		data.WriteBit(Extended);
		data.FlushBits();
	}

	public ulong InstanceID;
	public uint MapID;
	public uint DifficultyID;
	public int TimeRemaining;
	public uint CompletedMask;

	public bool Locked;
	public bool Extended;
}
