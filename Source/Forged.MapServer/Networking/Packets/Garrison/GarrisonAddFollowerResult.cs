// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GarrisonAddFollowerResult : ServerPacket
{
    public GarrisonType GarrTypeID;
    public GarrisonFollower Follower;
    public GarrisonError Result;
    public GarrisonAddFollowerResult() : base(ServerOpcodes.GarrisonAddFollowerResult, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteInt32((int)GarrTypeID);
        _worldPacket.WriteUInt32((uint)Result);
        Follower.Write(_worldPacket);
    }
}