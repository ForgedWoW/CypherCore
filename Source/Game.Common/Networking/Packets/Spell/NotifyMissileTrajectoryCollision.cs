// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Spell;

public class NotifyMissileTrajectoryCollision : ServerPacket
{
	public ObjectGuid Caster;
	public ObjectGuid CastID;
	public Vector3 CollisionPos;
	public NotifyMissileTrajectoryCollision() : base(ServerOpcodes.NotifyMissileTrajectoryCollision) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WriteVector3(CollisionPos);
	}
}
