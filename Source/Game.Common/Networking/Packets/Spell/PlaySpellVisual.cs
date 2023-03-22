// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class PlaySpellVisual : ServerPacket
{
	public ObjectGuid Source;
	public ObjectGuid Target;
	public ObjectGuid Transport;   // Used when Target = Empty && (SpellVisual::Flags & 0x400) == 0
	public Vector3 TargetPosition; // Overrides missile destination for SpellVisual::SpellVisualMissileSetID
	public uint SpellVisualID;
	public float TravelSpeed;
	public ushort HitReason;
	public ushort MissReason;
	public ushort ReflectStatus;
	public float LaunchDelay;
	public float MinDuration;
	public bool SpeedAsTime;
	public PlaySpellVisual() : base(ServerOpcodes.PlaySpellVisual) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Source);
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WritePackedGuid(Transport);
		_worldPacket.WriteVector3(TargetPosition);
		_worldPacket.WriteUInt32(SpellVisualID);
		_worldPacket.WriteFloat(TravelSpeed);
		_worldPacket.WriteUInt16(HitReason);
		_worldPacket.WriteUInt16(MissReason);
		_worldPacket.WriteUInt16(ReflectStatus);
		_worldPacket.WriteFloat(LaunchDelay);
		_worldPacket.WriteFloat(MinDuration);
		_worldPacket.WriteBit(SpeedAsTime);
		_worldPacket.FlushBits();
	}
}