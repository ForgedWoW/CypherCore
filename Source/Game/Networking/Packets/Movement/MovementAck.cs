// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public struct MovementAck
{
	public void Read(WorldPacket data)
	{
		Status = MovementExtensions.ReadMovementInfo(data);
		AckIndex = data.ReadInt32();
	}

	public MovementInfo Status;
	public int AckIndex;
}