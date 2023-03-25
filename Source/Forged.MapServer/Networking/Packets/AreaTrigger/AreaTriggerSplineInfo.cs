// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;

namespace Forged.MapServer.Networking.Packets.AreaTrigger;

class AreaTriggerSplineInfo
{
	public uint TimeToTarget;
	public uint ElapsedTimeForMovement;
	public List<Vector3> Points = new();

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(TimeToTarget);
		data.WriteUInt32(ElapsedTimeForMovement);

		data.WriteBits(Points.Count, 16);
		data.FlushBits();

		foreach (var point in Points)
			data.WriteVector3(point);
	}
}