// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Movement;

namespace Forged.MapServer.Networking.Packets.Vehicle;

public class MoveSetVehicleRecIdAck : ClientPacket
{
    public MovementAck Data;
    public int VehicleRecID;
    public MoveSetVehicleRecIdAck(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Data.Read(_worldPacket);
        VehicleRecID = _worldPacket.ReadInt32();
    }
}