// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Movement;

namespace Game.Common.Networking.Packets.Movement;

public class MoveUpdateSpeed : ServerPacket
{
	public MovementInfo Status;
	public float Speed = 1.0f;
	public MoveUpdateSpeed(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance) { }

	public override void Write()
	{
		MovementExtensions.WriteMovementInfo(_worldPacket, Status);
		_worldPacket.WriteFloat(Speed);
	}
}
