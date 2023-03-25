// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Garrison;

public struct GarrisonPlotInfo
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt32(GarrPlotInstanceID);
		data.WriteXYZO(PlotPos);
		data.WriteUInt32(PlotType);
	}

	public uint GarrPlotInstanceID;
	public Position PlotPos;
	public uint PlotType;
}