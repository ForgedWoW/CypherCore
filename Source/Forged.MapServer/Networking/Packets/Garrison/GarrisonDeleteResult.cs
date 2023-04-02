// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GarrisonDeleteResult : ServerPacket
{
    public uint GarrSiteID;
    public GarrisonError Result;
    public GarrisonDeleteResult() : base(ServerOpcodes.GarrisonDeleteResult, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32((uint)Result);
        WorldPacket.WriteUInt32(GarrSiteID);
    }
}