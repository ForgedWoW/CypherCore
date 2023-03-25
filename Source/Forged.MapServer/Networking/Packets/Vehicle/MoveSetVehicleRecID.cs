// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Vehicle;

public class MoveSetVehicleRecID : ServerPacket
{
	public ObjectGuid MoverGUID;
	public uint SequenceIndex;
	public uint VehicleRecID;
	public MoveSetVehicleRecID() : base(ServerOpcodes.MoveSetVehicleRecId) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(MoverGUID);
		_worldPacket.WriteUInt32(SequenceIndex);
		_worldPacket.WriteUInt32(VehicleRecID);
	}
}