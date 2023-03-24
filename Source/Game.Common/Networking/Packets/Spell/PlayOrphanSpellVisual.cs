// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Spell;

public class PlayOrphanSpellVisual : ServerPacket
{
	public ObjectGuid Target; // Exclusive with TargetLocation
	public Position SourceLocation;
	public uint SpellVisualID;
	public bool SpeedAsTime;
	public float TravelSpeed;
	public float LaunchDelay; // Always zero
	public float MinDuration;
	public Vector3 SourceRotation; // Vector of rotations, Orientation is z
	public Vector3 TargetLocation; // Exclusive with Target
	public PlayOrphanSpellVisual() : base(ServerOpcodes.PlayOrphanSpellVisual) { }

	public override void Write()
	{
		_worldPacket.WriteXYZ(SourceLocation);
		_worldPacket.WriteVector3(SourceRotation);
		_worldPacket.WriteVector3(TargetLocation);
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WriteUInt32(SpellVisualID);
		_worldPacket.WriteFloat(TravelSpeed);
		_worldPacket.WriteFloat(LaunchDelay);
		_worldPacket.WriteFloat(MinDuration);
		_worldPacket.WriteBit(SpeedAsTime);
		_worldPacket.FlushBits();
	}
}
