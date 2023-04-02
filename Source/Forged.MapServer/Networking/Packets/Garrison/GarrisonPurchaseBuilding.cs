// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GarrisonPurchaseBuilding : ClientPacket
{
    public uint BuildingID;
    public ObjectGuid NpcGUID;
    public uint PlotInstanceID;
    public GarrisonPurchaseBuilding(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        NpcGUID = WorldPacket.ReadPackedGuid();
        PlotInstanceID = WorldPacket.ReadUInt32();
        BuildingID = WorldPacket.ReadUInt32();
    }
}