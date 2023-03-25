// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Garrison;

class GarrisonPlaceBuildingResult : ServerPacket
{
	public GarrisonType GarrTypeID;
	public GarrisonError Result;
	public GarrisonBuildingInfo BuildingInfo = new();
	public bool PlayActivationCinematic;
	public GarrisonPlaceBuildingResult() : base(ServerOpcodes.GarrisonPlaceBuildingResult, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32((int)GarrTypeID);
		_worldPacket.WriteUInt32((uint)Result);
		BuildingInfo.Write(_worldPacket);
		_worldPacket.WriteBit(PlayActivationCinematic);
		_worldPacket.FlushBits();
	}
}