﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class MoveSplineSetSpeed : ServerPacket
{
	public ObjectGuid MoverGUID;
	public float Speed = 1.0f;
	public MoveSplineSetSpeed(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(MoverGUID);
		_worldPacket.WriteFloat(Speed);
	}
}