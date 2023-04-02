// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GarrisonPlotPlaced : ServerPacket
{
    public GarrisonType GarrTypeID;
    public GarrisonPlotInfo PlotInfo;
    public GarrisonPlotPlaced() : base(ServerOpcodes.GarrisonPlotPlaced, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteInt32((int)GarrTypeID);
        PlotInfo.Write(WorldPacket);
    }
}