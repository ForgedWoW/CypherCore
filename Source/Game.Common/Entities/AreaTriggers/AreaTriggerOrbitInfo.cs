// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Entities.AreaTriggers;

public class AreaTriggerOrbitInfo
{
	public ObjectGuid? PathTarget;
	public Vector3? Center;
	public bool CounterClockwise;
	public bool CanLoop;
	public uint TimeToTarget;
	public int ElapsedTimeForMovement;
	public uint StartDelay;
	public float Radius;
	public float BlendFromRadius;
	public float InitialAngle;
	public float ZOffset;

	public void Write(WorldPacket data)
	{
		data.WriteBit(PathTarget.HasValue);
		data.WriteBit(Center.HasValue);
		data.WriteBit(CounterClockwise);
		data.WriteBit(CanLoop);

		data.WriteUInt32(TimeToTarget);
		data.WriteInt32(ElapsedTimeForMovement);
		data.WriteUInt32(StartDelay);
		data.WriteFloat(Radius);
		data.WriteFloat(BlendFromRadius);
		data.WriteFloat(InitialAngle);
		data.WriteFloat(ZOffset);

		if (PathTarget.HasValue)
			data.WritePackedGuid(PathTarget.Value);

		if (Center.HasValue)
			data.WriteVector3(Center.Value);
	}
}
