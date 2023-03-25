// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

struct GarrisonBuildingMapData
{
	public GarrisonBuildingMapData(uint buildingPlotInstId, Position pos)
	{
		GarrBuildingPlotInstID = buildingPlotInstId;
		Pos = pos;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(GarrBuildingPlotInstID);
		data.WriteXYZ(Pos);
	}

	public uint GarrBuildingPlotInstID;
	public Position Pos;
}