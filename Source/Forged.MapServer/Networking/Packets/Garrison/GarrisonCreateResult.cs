// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GarrisonCreateResult : ServerPacket
{
    public uint GarrSiteLevelID;
    public uint Result;
    public GarrisonCreateResult() : base(ServerOpcodes.GarrisonCreateResult, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(Result);
        _worldPacket.WriteUInt32(GarrSiteLevelID);
    }
}

//Structs