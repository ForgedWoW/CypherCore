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
        WorldPacket.WriteUInt64(FollowerDBID);
        WorldPacket.WriteInt32(GarrTypeID);
        WorldPacket.WriteUInt32(Result);
        WorldPacket.WriteUInt32(Destroyed);
    }
}