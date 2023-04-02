// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MoveTeleport : ServerPacket
{
    public float Facing;
    public ObjectGuid MoverGUID;
    public Position Pos;
    public byte PreloadWorld;
    public uint SequenceIndex;
    public ObjectGuid? TransportGUID;
    public VehicleTeleport? Vehicle;
    public MoveTeleport() : base(ServerOpcodes.MoveTeleport, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(MoverGUID);
        WorldPacket.WriteUInt32(SequenceIndex);
        WorldPacket.WriteXYZ(Pos);
        WorldPacket.WriteFloat(Facing);
        WorldPacket.WriteUInt8(PreloadWorld);

        WorldPacket.WriteBit(TransportGUID.HasValue);
        WorldPacket.WriteBit(Vehicle.HasValue);
        WorldPacket.FlushBits();

        if (Vehicle.HasValue)
        {
            WorldPacket.WriteUInt8(Vehicle.Value.VehicleSeatIndex);
            WorldPacket.WriteBit(Vehicle.Value.VehicleExitVoluntary);
            WorldPacket.WriteBit(Vehicle.Value.VehicleExitTeleport);
            WorldPacket.FlushBits();
        }

        if (TransportGUID.HasValue)
            WorldPacket.WritePackedGuid(TransportGUID.Value);
    }
}