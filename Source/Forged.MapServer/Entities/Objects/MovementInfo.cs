// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Entities.Objects;

public class MovementInfo
{
    public AdvFlyingMovement? AdvFlying;
    public MovementInertia? Inertia;
    public JumpInfo Jump;
    public TransportInfo Transport;

    public MovementInfo()
    {
        Guid = ObjectGuid.Empty;
        MovementFlags = MovementFlag.None;
        MovementFlags2 = MovementFlag2.None;
        Time = 0;
        Pitch = 0.0f;

        Pos = new Position();
        Transport.Reset();
        Jump.Reset();
    }

    public MovementFlags3 ExtraMovementFlags2 { get; private set; }
    public ObjectGuid Guid { get; set; }
    public MovementFlag MovementFlags { get; set; }
    public MovementFlag2 MovementFlags2 { get; private set; }
    public float Pitch { get; set; }
    public Position Pos { get; set; }
    public float StepUpStartElevation { get; set; }
    public uint Time { get; set; }

    public void AddExtraMovementFlag2(MovementFlags3 flag)
    {
        ExtraMovementFlags2 |= flag;
    }

    public void AddMovementFlag(MovementFlag f)
    {
        MovementFlags |= f;
    }

    public void AddMovementFlag2(MovementFlag2 f)
    {
        MovementFlags2 |= f;
    }

    public bool HasExtraMovementFlag2(MovementFlags3 flag)
    {
        return (ExtraMovementFlags2 & flag) != 0;
    }

    public bool HasMovementFlag(MovementFlag f)
    {
        return (MovementFlags & f) != 0;
    }

    public bool HasMovementFlag2(MovementFlag2 f)
    {
        return (MovementFlags2 & f) != 0;
    }

    public void RemoveExtraMovementFlag2(MovementFlags3 flag)
    {
        ExtraMovementFlags2 &= ~flag;
    }

    public void RemoveMovementFlag(MovementFlag f)
    {
        MovementFlags &= ~f;
    }

    public void RemoveMovementFlag2(MovementFlag2 f)
    {
        MovementFlags2 &= ~f;
    }

    public void ResetJump()
    {
        Jump.Reset();
    }

    public void ResetTransport()
    {
        Transport.Reset();
    }

    public void SetExtraMovementFlags2(MovementFlags3 flag)
    {
        ExtraMovementFlags2 = flag;
    }

    public void SetFallTime(uint time)
    {
        Jump.FallTime = time;
    }

    public void SetMovementFlags2(MovementFlag2 f)
    {
        MovementFlags2 = f;
    }

    // advflying
    public struct AdvFlyingMovement
    {
        public float ForwardVelocity;
        public float UpVelocity;
    }

    public struct JumpInfo
    {
        public float CosAngle;

        public uint FallTime;

        public float SinAngle;

        public float Xyspeed;

        public float Zspeed;

        public void Reset()
        {
            FallTime = 0;
            Zspeed = SinAngle = CosAngle = Xyspeed = 0.0f;
        }
    }

    public struct MovementInertia
    {
        public Position Force;
        public int Id;
        public uint Lifetime;
    }

    public struct TransportInfo
    {
        public ObjectGuid Guid;

        public Position Pos;

        public uint PrevTime;

        public sbyte Seat;

        public uint Time;

        public uint VehicleId;

        public void Reset()
        {
            Guid = ObjectGuid.Empty;
            Pos = new Position();
            Seat = -1;
            Time = 0;
            PrevTime = 0;
            VehicleId = 0;
        }
    }
}