// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GarrisonUnlearnBlueprintResult : ServerPacket
{
    public uint BuildingID;
    public GarrisonType GarrTypeID;
    public GarrisonError Result;
    public GarrisonUnlearnBlueprintResult() : base(ServerOpcodes.GarrisonUnlearnBlueprintResult, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteInt32((int)GarrTypeID);
        WorldPacket.WriteUInt32((uint)Result);
        WorldPacket.WriteUInt32(BuildingID);
    }
}