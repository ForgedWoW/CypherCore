// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Garrison;

struct GarrisonRemoteBuildingInfo
{
	public GarrisonRemoteBuildingInfo(uint plotInstanceId, uint buildingId)
	{
		GarrPlotInstanceID = plotInstanceId;
		GarrBuildingID = buildingId;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(GarrPlotInstanceID);
		data.WriteUInt32(GarrBuildingID);
	}

	public uint GarrPlotInstanceID;
	public uint GarrBuildingID;
}