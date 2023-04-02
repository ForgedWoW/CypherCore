// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Garrison;

public class GarrisonBuildingInfo
{
    public bool Active;
    public uint CurrentGarSpecID;
    public uint GarrBuildingID;
    public uint GarrPlotInstanceID;
    public long TimeBuilt;
    public long TimeSpecCooldown = 2288912640; // 06/07/1906 18:35:44 - another in the series of magic blizz dates
    public void Write(WorldPacket data)
    {
        data.WriteUInt32(GarrPlotInstanceID);
        data.WriteUInt32(GarrBuildingID);
        data.WriteInt64(TimeBuilt);
        data.WriteUInt32(CurrentGarSpecID);
        data.WriteInt64(TimeSpecCooldown);
        data.WriteBit(Active);
        data.FlushBits();
    }
}