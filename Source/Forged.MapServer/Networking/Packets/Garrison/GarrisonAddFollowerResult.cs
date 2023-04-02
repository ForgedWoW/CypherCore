// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GarrisonAddFollowerResult : ServerPacket
{
    public GarrisonFollower Follower;
    public GarrisonType GarrTypeID;
    public GarrisonError Result;
    public GarrisonAddFollowerResult() : base(ServerOpcodes.GarrisonAddFollowerResult, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteInt32((int)GarrTypeID);
        WorldPacket.WriteUInt32((uint)Result);
        Follower.Write(WorldPacket);
    }
}