﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class ClientPlayerMovement : ClientPacket
{
	public MovementInfo Status;
	public ClientPlayerMovement(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Status = MovementExtensions.ReadMovementInfo(_worldPacket);
	}
}