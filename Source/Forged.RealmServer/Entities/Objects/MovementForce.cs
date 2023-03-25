// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets;

namespace Forged.RealmServer.Entities;

public class MovementForce
{
	public ObjectGuid ID { get; set; }
	public Vector3 Origin { get; set; }
	public Vector3 Direction { get; set; }
	public uint TransportID { get; set; }
	public float Magnitude { get; set; }
	public MovementForceType Type { get; set; }
	public int Unused910 { get; set; }

	public void Read(WorldPacket data)
	{
		ID = data.ReadPackedGuid();
		Origin = data.ReadVector3();
		Direction = data.ReadVector3();
		TransportID = data.ReadUInt32();
		Magnitude = data.ReadFloat();
		Unused910 = data.ReadInt32();
		Type = (MovementForceType)data.ReadBits<byte>(2);
	}

	public void Write(WorldPacket data)
	{
		MovementExtensions.WriteMovementForceWithDirection(this, data);
	}
}