﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GarrisonBuildingRemoved : ServerPacket
{
    public uint GarrBuildingID;
    public uint GarrPlotInstanceID;
    public GarrisonType GarrTypeID;
    public GarrisonError Result;
    public GarrisonBuildingRemoved() : base(ServerOpcodes.GarrisonBuildingRemoved, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteInt32((int)GarrTypeID);
        _worldPacket.WriteUInt32((uint)Result);
        _worldPacket.WriteUInt32(GarrPlotInstanceID);
        _worldPacket.WriteUInt32(GarrBuildingID);
    }
}