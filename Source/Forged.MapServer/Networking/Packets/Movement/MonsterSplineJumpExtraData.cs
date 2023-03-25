// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Movement;

public struct MonsterSplineJumpExtraData
{
	public float JumpGravity;
	public uint StartTime;
	public uint Duration;

	public void Write(WorldPacket data)
	{
		data.WriteFloat(JumpGravity);
		data.WriteUInt32(StartTime);
		data.WriteUInt32(Duration);
	}
}