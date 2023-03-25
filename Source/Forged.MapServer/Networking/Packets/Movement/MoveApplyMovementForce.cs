// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class MoveApplyMovementForce : ServerPacket
{
	public ObjectGuid MoverGUID;
	public int SequenceIndex;
	public MovementForce Force;
	public MoveApplyMovementForce() : base(ServerOpcodes.MoveApplyMovementForce, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(MoverGUID);
		_worldPacket.WriteInt32(SequenceIndex);
		Force.Write(_worldPacket);
	}
}