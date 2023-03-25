// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public struct MissileTrajectoryRequest
{
	public float Pitch;
	public float Speed;

	public void Read(WorldPacket data)
	{
		Pitch = data.ReadFloat();
		Speed = data.ReadFloat();
	}
}