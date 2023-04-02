// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Movement.Generators;
using Forged.MapServer.Networking.Packets.Movement;
using Framework.Constants;

namespace Forged.MapServer.Movement;

public class MoveSplineInit
{
    private readonly Unit _unit;

    public MoveSplineInitArgs Args { get; set; } = new();

    public MoveSplineInit(Unit m)
    {
        _unit = m;
        Args.SplineId = MotionMaster.SplineId;

        // Elevators also use MOVEMENTFLAG_ONTRANSPORT but we do not keep track of their position changes
        Args.TransformForTransport = !_unit.GetTransGUID().IsEmpty;
        // mix existing state into new
        Args.Flags.SetUnsetFlag(SplineFlag.CanSwim, _unit.CanSwim);
        Args.Walk = _unit.HasUnitMovementFlag(MovementFlag.Walking);
        Args.Flags.SetUnsetFlag(SplineFlag.Flying, _unit.HasUnitMovementFlag(MovementFlag.CanFly | MovementFlag.DisableGravity));
        Args.Flags.SetUnsetFlag(SplineFlag.SmoothGroundPath); // enabled by default, CatmullRom mode or client config "pathSmoothing" will disable this
        Args.Flags.SetUnsetFlag(SplineFlag.Steering, _unit.HasNpcFlag2(NPCFlags2.Steering));
    }

    public void DisableTransportPathTransformations()
    {
        Args.TransformForTransport = false;
    }

    public int Launch()
    {
        var moveSpline = _unit.MoveSpline;

        var transport = !_unit.GetTransGUID().IsEmpty;
        Vector4 realPosition = new();

        // there is a big chance that current position is unknown if current state is not finalized, need compute it
        // this also allows calculate spline position and update map position in much greater intervals
        // Don't compute for transport movement if the unit is in a motion between two transports
        if (!moveSpline.Finalized() && moveSpline.OnTransport == transport)
        {
            realPosition = moveSpline.ComputePosition();
        }
        else
        {
            Position pos;

            if (!transport)
                pos = _unit.Location;
            else
                pos = _unit.MovementInfo.Transport.Pos;

            realPosition.X = pos.X;
            realPosition.Y = pos.Y;
            realPosition.Z = pos.Z;
            realPosition.W = _unit.Location.Orientation;
        }

        // should i do the things that user should do? - no.
        if (Args.Path.Count == 0)
            return 0;

        // correct first vertex
        Args.Path[0] = new Vector3(realPosition.X, realPosition.Y, realPosition.Z);
        Args.InitialOrientation = realPosition.W;
        Args.Flags.SetUnsetFlag(SplineFlag.EnterCycle, Args.Flags.HasFlag(SplineFlag.Cyclic));
        moveSpline.OnTransport = transport;

        var moveFlags = _unit.MovementInfo.MovementFlags;

        if (!Args.Flags.HasFlag(SplineFlag.Backward))
            moveFlags = (moveFlags & ~MovementFlag.Backward) | MovementFlag.Forward;
        else
            moveFlags = (moveFlags & ~MovementFlag.Forward) | MovementFlag.Backward;

        if (Convert.ToBoolean(moveFlags & MovementFlag.Root))
            moveFlags &= ~MovementFlag.MaskMoving;

        if (!Args.HasVelocity)
        {
            // If spline is initialized with SetWalk method it only means we need to select
            // walk move speed for it but not add walk flag to unit
            var moveFlagsForSpeed = moveFlags;

            if (Args.Walk)
                moveFlagsForSpeed |= MovementFlag.Walking;
            else
                moveFlagsForSpeed &= ~MovementFlag.Walking;

            Args.Velocity = _unit.GetSpeed(SelectSpeedType(moveFlagsForSpeed));
            var creature = _unit.AsCreature;

            if (creature is { HasSearchedAssistance: true })
                Args.Velocity *= 0.66f;
        }

        // limit the speed in the same way the client does
        float SpeedLimit()
        {
            if (Args.Flags.HasFlag(SplineFlag.UnlimitedSpeed))
                return float.MaxValue;

            if (Args.Flags.HasFlag(SplineFlag.Falling) || Args.Flags.HasFlag(SplineFlag.Catmullrom) || Args.Flags.HasFlag(SplineFlag.Flying) || Args.Flags.HasFlag(SplineFlag.Parabolic))
                return 50.0f;

            return Math.Max(28.0f, _unit.GetSpeed(UnitMoveType.Run) * 4.0f);
        }

        ;

        Args.Velocity = Math.Min(Args.Velocity, SpeedLimit());

        if (!Args.Validate(_unit))
            return 0;

        _unit.MovementInfo.MovementFlags = moveFlags;
        moveSpline.Initialize(Args);

        MonsterMove packet = new()
        {
            MoverGUID = _unit.GUID,
            Pos = new Vector3(realPosition.X, realPosition.Y, realPosition.Z)
        };

        packet.InitializeSplineData(moveSpline);

        if (transport)
        {
            packet.SplineData.Move.TransportGUID = _unit.GetTransGUID();
            packet.SplineData.Move.VehicleSeat = _unit.MovementInfo.Transport.Seat;
        }

        _unit.SendMessageToSet(packet, true);

        return moveSpline.Duration();
    }

    public void MovebyPath(Vector3[] controls, int pathOffset = 0)
    {
        Args.PathIdxOffset = pathOffset;
        TransportPathTransform transform = new(_unit, Args.TransformForTransport);

        for (var i = 0; i < controls.Length; i++)
            Args.Path.Add(transform.Calc(controls[i]));
    }

    public void MoveTo(Vector3 dest, bool generatePath = true, bool forceDestination = false)
    {
        if (generatePath)
        {
            PathGenerator path = new(_unit);
            var result = path.CalculatePath(new Position(dest), forceDestination);

            if (result && !Convert.ToBoolean(path.GetPathType() & PathType.NoPath))
            {
                MovebyPath(path.GetPath());

                return;
            }
        }

        Args.PathIdxOffset = 0;
        Args.Path.Add(default);
        TransportPathTransform transform = new(_unit, Args.TransformForTransport);
        Args.Path.Add(transform.Calc(dest));
    }

    public void MoveTo(float x, float y, float z, bool generatePath = true, bool forceDest = false)
    {
        MoveTo(new Vector3(x, y, z), generatePath, forceDest);
    }

    public List<Vector3> Path()
    {
        return Args.Path;
    }

    public void SetAnimation(AnimTier anim)
    {
        Args.TimePerc = 0.0f;

        Args.AnimTier = new AnimTierTransition
        {
            AnimTier = (byte)anim
        };

        Args.Flags.EnableAnimation();
    }

    public void SetCyclic()
    {
        Args.Flags.SetUnsetFlag(SplineFlag.Cyclic);
    }

    public void SetFacing(Unit target)
    {
        Args.Facing.Angle = _unit.Location.GetAbsoluteAngle(target.Location);
        Args.Facing.Target = target.GUID;
        Args.Facing.Type = MonsterMoveType.FacingTarget;
    }

    public void SetFacing(float angle)
    {
        if (Args.TransformForTransport)
        {
            var vehicle = _unit.VehicleBase;

            if (vehicle != null)
            {
                angle -= vehicle.Location.Orientation;
            }
            else
            {
                var transport = _unit.Transport;

                if (transport != null)
                    angle -= transport.GetTransportOrientation();
            }
        }

        Args.Facing.Angle = MathFunctions.wrap(angle, 0.0f, MathFunctions.TwoPi);
        Args.Facing.Type = MonsterMoveType.FacingAngle;
    }

    public void SetFacing(Vector3 spot)
    {
        TransportPathTransform transform = new(_unit, Args.TransformForTransport);
        var finalSpot = transform.Calc(spot);
        Args.Facing.F = new Vector3(finalSpot.X, finalSpot.Y, finalSpot.Z);
        Args.Facing.Type = MonsterMoveType.FacingSpot;
    }

    public void SetFall()
    {
        Args.Flags.EnableFalling();
        Args.Flags.SetUnsetFlag(SplineFlag.FallingSlow, _unit.HasUnitMovementFlag(MovementFlag.FallingSlow));
    }

    public void SetFirstPointId(int pointId)
    {
        Args.PathIdxOffset = pointId;
    }

    public void SetFly()
    {
        Args.Flags.EnableFlying();
    }

    public void SetOrientationFixed(bool enable)
    {
        Args.Flags.SetUnsetFlag(SplineFlag.OrientationFixed, enable);
    }

    public void SetParabolic(float amplitude, float timeShift)
    {
        Args.TimePerc = timeShift;
        Args.ParabolicAmplitude = amplitude;
        Args.VerticalAcceleration = 0.0f;
        Args.Flags.EnableParabolic();
    }

    public void SetParabolicVerticalAcceleration(float verticalAcceleration, float timeShift)
    {
        Args.TimePerc = timeShift;
        Args.ParabolicAmplitude = 0.0f;
        Args.VerticalAcceleration = verticalAcceleration;
        Args.Flags.EnableParabolic();
    }

    public void SetSmooth()
    {
        Args.Flags.EnableCatmullRom();
    }

    public void SetSpellEffectExtraData(SpellEffectExtraData spellEffectExtraData)
    {
        Args.SpellEffectExtra = spellEffectExtraData;
    }

    public void SetTransportEnter()
    {
        Args.Flags.EnableTransportEnter();
    }

    public void SetTransportExit()
    {
        Args.Flags.EnableTransportExit();
    }

    public void SetUncompressed()
    {
        Args.Flags.SetUnsetFlag(SplineFlag.UncompressedPath);
    }

    public void SetUnlimitedSpeed()
    {
        Args.Flags.SetUnsetFlag(SplineFlag.UnlimitedSpeed);
    }

    public void SetVelocity(float vel)
    {
        Args.Velocity = vel;
        Args.HasVelocity = true;
    }

    public void SetWalk(bool enable)
    {
        Args.Walk = enable;
    }

    public void Stop()
    {
        var moveSpline = _unit.MoveSpline;

        // No need to stop if we are not moving
        if (moveSpline.Finalized())
            return;

        var transport = !_unit.GetTransGUID().IsEmpty;
        Vector4 loc = new();

        if (moveSpline.OnTransport == transport)
        {
            loc = moveSpline.ComputePosition();
        }
        else
        {
            Position pos;

            if (!transport)
                pos = _unit.Location;
            else
                pos = _unit.MovementInfo.Transport.Pos;

            loc.X = pos.X;
            loc.Y = pos.Y;
            loc.Z = pos.Z;
            loc.W = _unit.Location.Orientation;
        }

        Args.Flags.Flags = SplineFlag.Done;
        _unit.MovementInfo.RemoveMovementFlag(MovementFlag.Forward);
        moveSpline.OnTransport = transport;
        moveSpline.Initialize(Args);

        MonsterMove packet = new()
        {
            MoverGUID = _unit.GUID,
            Pos = new Vector3(loc.X, loc.Y, loc.Z),
            SplineData =
            {
                StopDistanceTolerance = 2,
                Id = moveSpline.GetId()
            }
        };

        if (transport)
        {
            packet.SplineData.Move.TransportGUID = _unit.GetTransGUID();
            packet.SplineData.Move.VehicleSeat = _unit.MovementInfo.Transport.Seat;
        }

        _unit.SendMessageToSet(packet, true);
    }

    private UnitMoveType SelectSpeedType(MovementFlag moveFlags)
    {
        if (moveFlags.HasAnyFlag(MovementFlag.Flying))
        {
            if (moveFlags.HasAnyFlag(MovementFlag.Backward))
                return UnitMoveType.FlightBack;
            else
                return UnitMoveType.Flight;
        }
        else if (moveFlags.HasAnyFlag(MovementFlag.Swimming))
        {
            if (moveFlags.HasAnyFlag(MovementFlag.Backward))
                return UnitMoveType.SwimBack;
            else
                return UnitMoveType.Swim;
        }
        else if (moveFlags.HasAnyFlag(MovementFlag.Walking))
        {
            return UnitMoveType.Walk;
        }
        else if (moveFlags.HasAnyFlag(MovementFlag.Backward))
        {
            return UnitMoveType.RunBack;
        }

        // Flying creatures use MOVEMENTFLAG_CAN_FLY or MOVEMENTFLAG_DISABLE_GRAVITY
        // Run speed is their default flight speed.
        return UnitMoveType.Run;
    }

    private void SetBackward()
    {
        Args.Flags.SetUnsetFlag(SplineFlag.Backward);
    }
}

// Transforms coordinates from global to transport offsets