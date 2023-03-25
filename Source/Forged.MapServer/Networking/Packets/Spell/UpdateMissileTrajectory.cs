// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Movement;

namespace Forged.MapServer.Networking.Packets.Spell;

class UpdateMissileTrajectory : ClientPacket
{
	public ObjectGuid Guid;
	public ObjectGuid CastID;
	public ushort MoveMsgID;
	public uint SpellID;
	public float Pitch;
	public float Speed;
	public Vector3 FirePos;
	public Vector3 ImpactPos;
	public MovementInfo Status;
	public UpdateMissileTrajectory(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Guid = _worldPacket.ReadPackedGuid();
		CastID = _worldPacket.ReadPackedGuid();
		MoveMsgID = _worldPacket.ReadUInt16();
		SpellID = _worldPacket.ReadUInt32();
		Pitch = _worldPacket.ReadFloat();
		Speed = _worldPacket.ReadFloat();
		FirePos = _worldPacket.ReadVector3();
		ImpactPos = _worldPacket.ReadVector3();
		var hasStatus = _worldPacket.HasBit();

		_worldPacket.ResetBitPos();

		if (hasStatus)
			Status = MovementExtensions.ReadMovementInfo(_worldPacket);
	}
}