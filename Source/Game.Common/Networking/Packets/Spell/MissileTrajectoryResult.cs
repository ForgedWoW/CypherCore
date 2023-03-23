// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Spell;

public struct MissileTrajectoryResult
{
	public uint TravelTime;
	public float Pitch;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(TravelTime);
		data.WriteFloat(Pitch);
	}
}
