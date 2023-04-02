// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal struct GarrisonBuildingMapData
{
    public uint GarrBuildingPlotInstID;

    public Position Pos;

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
}