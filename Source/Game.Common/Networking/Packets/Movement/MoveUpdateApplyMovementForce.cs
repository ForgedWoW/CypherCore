// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Movement;

namespace Game.Common.Networking.Packets.Movement;

public class MoveUpdateApplyMovementForce : ServerPacket
{
	public MovementInfo Status = new();
	public MovementForce Force = new();
	public MoveUpdateApplyMovementForce() : base(ServerOpcodes.MoveUpdateApplyMovementForce) { }

	public override void Write()
	{
		MovementExtensions.WriteMovementInfo(_worldPacket, Status);
		Force.Write(_worldPacket);
	}
}
