// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GarrisonPlaceBuildingResult : ServerPacket
{
    public GarrisonBuildingInfo BuildingInfo = new();
    public GarrisonType GarrTypeID;
    public bool PlayActivationCinematic;
    public GarrisonError Result;
    public GarrisonPlaceBuildingResult() : base(ServerOpcodes.GarrisonPlaceBuildingResult, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteInt32((int)GarrTypeID);
        WorldPacket.WriteUInt32((uint)Result);
        BuildingInfo.Write(WorldPacket);
        WorldPacket.WriteBit(PlayActivationCinematic);
        WorldPacket.FlushBits();
    }
}