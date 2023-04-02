// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MoveUpdateTeleport : ServerPacket
{
    public float? FlightBackSpeed;
    public float? FlightSpeed;
    public List<MovementForce> MovementForces;
    public float? PitchRate;
    public float? RunBackSpeed;
    public float? RunSpeed;
    public MovementInfo Status;
    public float? SwimBackSpeed;
    public float? SwimSpeed;
    public float? TurnRate;
    public float? WalkSpeed;
    public MoveUpdateTeleport() : base(ServerOpcodes.MoveUpdateTeleport) { }

    public override void Write()
    {
        MovementExtensions.WriteMovementInfo(WorldPacket, Status);

        WorldPacket.WriteInt32(MovementForces?.Count ?? 0);
        WorldPacket.WriteBit(WalkSpeed.HasValue);
        WorldPacket.WriteBit(RunSpeed.HasValue);
        WorldPacket.WriteBit(RunBackSpeed.HasValue);
        WorldPacket.WriteBit(SwimSpeed.HasValue);
        WorldPacket.WriteBit(SwimBackSpeed.HasValue);
        WorldPacket.WriteBit(FlightSpeed.HasValue);
        WorldPacket.WriteBit(FlightBackSpeed.HasValue);
        WorldPacket.WriteBit(TurnRate.HasValue);
        WorldPacket.WriteBit(PitchRate.HasValue);
        WorldPacket.FlushBits();

        if (MovementForces != null)
            foreach (var force in MovementForces)
                force.Write(WorldPacket);

        if (WalkSpeed.HasValue)
            WorldPacket.WriteFloat(WalkSpeed.Value);

        if (RunSpeed.HasValue)
            WorldPacket.WriteFloat(RunSpeed.Value);

        if (RunBackSpeed.HasValue)
            WorldPacket.WriteFloat(RunBackSpeed.Value);

        if (SwimSpeed.HasValue)
            WorldPacket.WriteFloat(SwimSpeed.Value);

        if (SwimBackSpeed.HasValue)
            WorldPacket.WriteFloat(SwimBackSpeed.Value);

        if (FlightSpeed.HasValue)
            WorldPacket.WriteFloat(FlightSpeed.Value);

        if (FlightBackSpeed.HasValue)
            WorldPacket.WriteFloat(FlightBackSpeed.Value);

        if (TurnRate.HasValue)
            WorldPacket.WriteFloat(TurnRate.Value);

        if (PitchRate.HasValue)
            WorldPacket.WriteFloat(PitchRate.Value);
    }
}