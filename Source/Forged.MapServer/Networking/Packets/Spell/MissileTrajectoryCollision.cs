// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class MissileTrajectoryCollision : ClientPacket
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