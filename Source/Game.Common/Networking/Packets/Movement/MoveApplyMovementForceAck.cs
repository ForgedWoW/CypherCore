// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
using Game.Common.Networking.Packets.Movement;

namespace Game.Common.Networking.Packets.Movement;

public class MoveApplyMovementForceAck : ClientPacket
{
	public MovementAck Ack = new();
	public MovementForce Force = new();
	public MoveApplyMovementForceAck(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Ack.Read(_worldPacket);
		Force.Read(_worldPacket);
	}
}
