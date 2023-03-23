// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Spell;

public class MissileTrajectoryCollision : ClientPacket
{
	public ObjectGuid Target;
	public uint SpellID;
	public ObjectGuid CastID;
	public Vector3 CollisionPos;
	public MissileTrajectoryCollision(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Target = _worldPacket.ReadPackedGuid();
		SpellID = _worldPacket.ReadUInt32();
		CastID = _worldPacket.ReadPackedGuid();
		CollisionPos = _worldPacket.ReadVector3();
	}
}
