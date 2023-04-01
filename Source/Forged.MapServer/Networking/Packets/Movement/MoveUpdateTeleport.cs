// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MoveUpdateTeleport : ServerPacket
{
    public MovementInfo Status;
    public List<MovementForce> MovementForces;
    public float? SwimBackSpeed;
    public float? FlightSpeed;
    public float? SwimSpeed;
    public float? WalkSpeed;
    public float? TurnRate;
    public float? RunSpeed;
    public float? FlightBackSpeed;
    public float? RunBackSpeed;
    public float? PitchRate;
    public MoveUpdateTeleport() : base(ServerOpcodes.MoveUpdateTeleport) { }

    public override void Write()
    {
        MovementExtensions.WriteMovementInfo(_worldPacket, Status);

        _worldPacket.WriteInt32(MovementForces?.Count ?? 0);
        _worldPacket.WriteBit(WalkSpeed.HasValue);
        _worldPacket.WriteBit(RunSpeed.HasValue);
        _worldPacket.WriteBit(RunBackSpeed.HasValue);
        _worldPacket.WriteBit(SwimSpeed.HasValue);
        _worldPacket.WriteBit(SwimBackSpeed.HasValue);
        _worldPacket.WriteBit(FlightSpeed.HasValue);
        _worldPacket.WriteBit(FlightBackSpeed.HasValue);
        _worldPacket.WriteBit(TurnRate.HasValue);
        _worldPacket.WriteBit(PitchRate.HasValue);
        _worldPacket.FlushBits();

        if (MovementForces != null)
            foreach (var force in MovementForces)
                force.Write(_worldPacket);

        if (WalkSpeed.HasValue)
            _worldPacket.WriteFloat(WalkSpeed.Value);

        if (RunSpeed.HasValue)
            _worldPacket.WriteFloat(RunSpeed.Value);

        if (RunBackSpeed.HasValue)
            _worldPacket.WriteFloat(RunBackSpeed.Value);

        if (SwimSpeed.HasValue)
            _worldPacket.WriteFloat(SwimSpeed.Value);

        if (SwimBackSpeed.HasValue)
            _worldPacket.WriteFloat(SwimBackSpeed.Value);

        if (FlightSpeed.HasValue)
            _worldPacket.WriteFloat(FlightSpeed.Value);

        if (FlightBackSpeed.HasValue)
            _worldPacket.WriteFloat(FlightBackSpeed.Value);

        if (TurnRate.HasValue)
            _worldPacket.WriteFloat(TurnRate.Value);

        if (PitchRate.HasValue)
            _worldPacket.WriteFloat(PitchRate.Value);
    }
}