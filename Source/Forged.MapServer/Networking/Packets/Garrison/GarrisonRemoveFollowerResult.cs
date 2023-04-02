// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GarrisonRemoveFollowerResult : ServerPacket
{
    public uint Destroyed;
    public ulong FollowerDBID;
    public int GarrTypeID;
    public uint Result;
    public GarrisonRemoveFollowerResult() : base(ServerOpcodes.GarrisonRemoveFollowerResult) { }

    public override void Write()
    {
        _worldPacket.WriteUInt64(FollowerDBID);
        _worldPacket.WriteInt32(GarrTypeID);
        _worldPacket.WriteUInt32(Result);
        _worldPacket.WriteUInt32(Destroyed);
    }
}