// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Movement;

namespace Forged.MapServer.Networking.Packets.Vehicle;

public class MoveChangeVehicleSeats : ClientPacket
{
    public byte DstSeatIndex = 255;
    public ObjectGuid DstVehicle;
    public MovementInfo Status;
    public MoveChangeVehicleSeats(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Status = MovementExtensions.ReadMovementInfo(WorldPacket);
        DstVehicle = WorldPacket.ReadPackedGuid();
        DstSeatIndex = WorldPacket.ReadUInt8();
    }
}