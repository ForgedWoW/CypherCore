// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class MoveTeleport : ServerPacket
{
	public Position Pos;
	public VehicleTeleport? Vehicle;
	public uint SequenceIndex;
	public ObjectGuid MoverGUID;
	public ObjectGuid? TransportGUID;
	public float Facing;
	public byte PreloadWorld;
	public MoveTeleport() : base(ServerOpcodes.MoveTeleport, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(MoverGUID);
		_worldPacket.WriteUInt32(SequenceIndex);
		_worldPacket.WriteXYZ(Pos);
		_worldPacket.WriteFloat(Facing);
		_worldPacket.WriteUInt8(PreloadWorld);

		_worldPacket.WriteBit(TransportGUID.HasValue);
		_worldPacket.WriteBit(Vehicle.HasValue);
		_worldPacket.FlushBits();

		if (Vehicle.HasValue)
		{
			_worldPacket.WriteUInt8(Vehicle.Value.VehicleSeatIndex);
			_worldPacket.WriteBit(Vehicle.Value.VehicleExitVoluntary);
			_worldPacket.WriteBit(Vehicle.Value.VehicleExitTeleport);
			_worldPacket.FlushBits();
		}

		if (TransportGUID.HasValue)
			_worldPacket.WritePackedGuid(TransportGUID.Value);
	}
}