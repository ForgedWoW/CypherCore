// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Vehicle;

public class SetVehicleRecID : ServerPacket
{
	public ObjectGuid VehicleGUID;
	public uint VehicleRecID;
	public SetVehicleRecID() : base(ServerOpcodes.SetVehicleRecId, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(VehicleGUID);
		_worldPacket.WriteUInt32(VehicleRecID);
	}
}
