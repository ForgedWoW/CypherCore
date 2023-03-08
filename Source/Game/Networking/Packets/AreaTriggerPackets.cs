// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class AreaTriggerPkt : ClientPacket
{
	public uint AreaTriggerID;
	public bool Entered;
	public bool FromClient;
	public AreaTriggerPkt(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		AreaTriggerID = _worldPacket.ReadUInt32();
		Entered = _worldPacket.HasBit();
		FromClient = _worldPacket.HasBit();
	}
}

class AreaTriggerDenied : ServerPacket
{
	public int AreaTriggerID;
	public bool Entered;
	public AreaTriggerDenied() : base(ServerOpcodes.AreaTriggerDenied) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(AreaTriggerID);
		_worldPacket.WriteBit(Entered);
		_worldPacket.FlushBits();
	}
}

class AreaTriggerNoCorpse : ServerPacket
{
	public AreaTriggerNoCorpse() : base(ServerOpcodes.AreaTriggerNoCorpse) { }

	public override void Write() { }
}

class AreaTriggerRePath : ServerPacket
{
	public AreaTriggerSplineInfo AreaTriggerSpline;
	public AreaTriggerOrbitInfo AreaTriggerOrbit;
	public AreaTriggerMovementScriptInfo? AreaTriggerMovementScript;
	public ObjectGuid TriggerGUID;
	public AreaTriggerRePath() : base(ServerOpcodes.AreaTriggerRePath) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(TriggerGUID);

		_worldPacket.WriteBit(AreaTriggerSpline != null);
		_worldPacket.WriteBit(AreaTriggerOrbit != null);
		_worldPacket.WriteBit(AreaTriggerMovementScript.HasValue);
		_worldPacket.FlushBits();

		if (AreaTriggerSpline != null)
			AreaTriggerSpline.Write(_worldPacket);

		if (AreaTriggerMovementScript.HasValue)
			AreaTriggerMovementScript.Value.Write(_worldPacket);

		if (AreaTriggerOrbit != null)
			AreaTriggerOrbit.Write(_worldPacket);
	}
}

//Structs
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