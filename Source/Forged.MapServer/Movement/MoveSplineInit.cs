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
	public MoveSplineInitArgs args = new();
    private readonly Unit unit;

	public MoveSplineInit(Unit m)
	{
		unit = m;
		args.splineId = MotionMaster.SplineId;

		// Elevators also use MOVEMENTFLAG_ONTRANSPORT but we do not keep track of their position changes
		args.TransformForTransport = !unit.GetTransGUID().IsEmpty;
		// mix existing state into new
		args.flags.SetUnsetFlag(SplineFlag.CanSwim, unit.CanSwim);
		args.walk = unit.HasUnitMovementFlag(MovementFlag.Walking);
		args.flags.SetUnsetFlag(SplineFlag.Flying, unit.HasUnitMovementFlag(MovementFlag.CanFly | MovementFlag.DisableGravity));
		args.flags.SetUnsetFlag(SplineFlag.SmoothGroundPath, true); // enabled by default, CatmullRom mode or client config "pathSmoothing" will disable this
		args.flags.SetUnsetFlag(SplineFlag.Steering, unit.HasNpcFlag2(NPCFlags2.Steering));
	}

	public int Launch()
	{
		var move_spline = unit.MoveSpline;

		var transport = !unit.GetTransGUID().IsEmpty;
		Vector4 real_position = new();

		// there is a big chance that current position is unknown if current state is not finalized, need compute it
		// this also allows calculate spline position and update map position in much greater intervals
		// Don't compute for transport movement if the unit is in a motion between two transports
		if (!move_spline.Finalized() && move_spline.onTransport == transport)
		{
			real_position = move_spline.ComputePosition();
		}
		else
		{
			Position pos;

			if (!transport)
				pos = unit.Location;
			else
				pos = unit.MovementInfo.Transport.Pos;

			real_position.X = pos.X;
			real_position.Y = pos.Y;
			real_position.Z = pos.Z;
			real_position.W = unit.Location.Orientation;
		}

		// should i do the things that user should do? - no.
		if (args.path.Count == 0)
			return 0;

		// correct first vertex
		args.path[0] = new Vector3(real_position.X, real_position.Y, real_position.Z);
		args.initialOrientation = real_position.W;
		args.flags.SetUnsetFlag(SplineFlag.EnterCycle, args.flags.HasFlag(SplineFlag.Cyclic));
		move_spline.onTransport = transport;

		var moveFlags = unit.MovementInfo.MovementFlags;

		if (!args.flags.HasFlag(SplineFlag.Backward))
			moveFlags = (moveFlags & ~MovementFlag.Backward) | MovementFlag.Forward;
		else
			moveFlags = (moveFlags & ~MovementFlag.Forward) | MovementFlag.Backward;

		if (Convert.ToBoolean(moveFlags & MovementFlag.Root))
			moveFlags &= ~MovementFlag.MaskMoving;

		if (!args.HasVelocity)
		{
			// If spline is initialized with SetWalk method it only means we need to select
			// walk move speed for it but not add walk flag to unit
			var moveFlagsForSpeed = moveFlags;

			if (args.walk)
				moveFlagsForSpeed |= MovementFlag.Walking;
			else
				moveFlagsForSpeed &= ~MovementFlag.Walking;

			args.velocity = unit.GetSpeed(SelectSpeedType(moveFlagsForSpeed));
			var creature = unit.AsCreature;

			if (creature is { HasSearchedAssistance: true })
				args.velocity *= 0.66f;
		}

		// limit the speed in the same way the client does
		float speedLimit()
		{
			if (args.flags.HasFlag(SplineFlag.UnlimitedSpeed))
				return float.MaxValue;

			if (args.flags.HasFlag(SplineFlag.Falling) || args.flags.HasFlag(SplineFlag.Catmullrom) || args.flags.HasFlag(SplineFlag.Flying) || args.flags.HasFlag(SplineFlag.Parabolic))
				return 50.0f;

			return Math.Max(28.0f, unit.GetSpeed(UnitMoveType.Run) * 4.0f);
		}

		;

		args.velocity = Math.Min(args.velocity, speedLimit());

		if (!args.Validate(unit))
			return 0;

		unit.MovementInfo.MovementFlags = moveFlags;
		move_spline.Initialize(args);

		MonsterMove packet = new()
		{
			MoverGUID = unit.GUID,
			Pos = new Vector3(real_position.X, real_position.Y, real_position.Z)
		};

		packet.InitializeSplineData(move_spline);

		if (transport)
		{
			packet.SplineData.Move.TransportGUID = unit.GetTransGUID();
			packet.SplineData.Move.VehicleSeat = unit.TransSeat;
		}

		unit.SendMessageToSet(packet, true);

		return move_spline.Duration();
	}

	public void Stop()
	{
		var move_spline = unit.MoveSpline;

		// No need to stop if we are not moving
		if (move_spline.Finalized())
			return;

		var transport = !unit.GetTransGUID().IsEmpty;
		Vector4 loc = new();

		if (move_spline.onTransport == transport)
		{
			loc = move_spline.ComputePosition();
		}
		else
		{
			Position pos;

			if (!transport)
				pos = unit.Location;
			else
				pos = unit.MovementInfo.Transport.Pos;

			loc.X = pos.X;
			loc.Y = pos.Y;
			loc.Z = pos.Z;
			loc.W = unit.Location.Orientation;
		}

		args.flags.Flags = SplineFlag.Done;
		unit.MovementInfo.RemoveMovementFlag(MovementFlag.Forward);
		move_spline.onTransport = transport;
		move_spline.Initialize(args);

		MonsterMove packet = new()
		{
			MoverGUID = unit.GUID,
			Pos = new Vector3(loc.X, loc.Y, loc.Z),
			SplineData =
			{
				StopDistanceTolerance = 2,
				Id = move_spline.GetId()
			}
		};

		if (transport)
		{
			packet.SplineData.Move.TransportGUID = unit.GetTransGUID();
			packet.SplineData.Move.VehicleSeat = unit.TransSeat;
		}

		unit.SendMessageToSet(packet, true);
	}

	public void SetFacing(Unit target)
	{
		args.facing.angle = unit.Location.GetAbsoluteAngle(target.Location);
		args.facing.target = target.GUID;
		args.facing.type = MonsterMoveType.FacingTarget;
	}

	public void SetFacing(float angle)
	{
		if (args.TransformForTransport)
		{
			var vehicle = unit.VehicleBase;

			if (vehicle != null)
			{
				angle -= vehicle.Location.Orientation;
			}
			else
			{
				var transport = unit.Transport;

				if (transport != null)
					angle -= transport.GetTransportOrientation();
			}
		}

		args.facing.angle = MathFunctions.wrap(angle, 0.0f, MathFunctions.TwoPi);
		args.facing.type = MonsterMoveType.FacingAngle;
	}

	public void MoveTo(Vector3 dest, bool generatePath = true, bool forceDestination = false)
	{
		if (generatePath)
		{
			PathGenerator path = new(unit);
			var result = path.CalculatePath(new Position(dest), forceDestination);

			if (result && !Convert.ToBoolean(path.GetPathType() & PathType.NoPath))
			{
				MovebyPath(path.GetPath());

				return;
			}
		}

		args.path_Idx_offset = 0;
		args.path.Add(default);
		TransportPathTransform transform = new(unit, args.TransformForTransport);
		args.path.Add(transform.Calc(dest));
	}

	public void SetFall()
	{
		args.flags.EnableFalling();
		args.flags.SetUnsetFlag(SplineFlag.FallingSlow, unit.HasUnitMovementFlag(MovementFlag.FallingSlow));
	}

	public void SetFirstPointId(int pointId)
	{
		args.path_Idx_offset = pointId;
	}

	public void SetFly()
	{
		args.flags.EnableFlying();
	}

	public void SetWalk(bool enable)
	{
		args.walk = enable;
	}

	public void SetSmooth()
	{
		args.flags.EnableCatmullRom();
	}

	public void SetUncompressed()
	{
		args.flags.SetUnsetFlag(SplineFlag.UncompressedPath);
	}

	public void SetCyclic()
	{
		args.flags.SetUnsetFlag(SplineFlag.Cyclic);
	}

	public void SetVelocity(float vel)
	{
		args.velocity = vel;
		args.HasVelocity = true;
	}

	public void SetTransportEnter()
	{
		args.flags.EnableTransportEnter();
	}

	public void SetTransportExit()
	{
		args.flags.EnableTransportExit();
	}

	public void SetOrientationFixed(bool enable)
	{
		args.flags.SetUnsetFlag(SplineFlag.OrientationFixed, enable);
	}

	public void SetUnlimitedSpeed()
	{
		args.flags.SetUnsetFlag(SplineFlag.UnlimitedSpeed, true);
	}

	public void MovebyPath(Vector3[] controls, int path_offset = 0)
	{
		args.path_Idx_offset = path_offset;
		TransportPathTransform transform = new(unit, args.TransformForTransport);

		for (var i = 0; i < controls.Length; i++)
			args.path.Add(transform.Calc(controls[i]));
	}

	public void MoveTo(float x, float y, float z, bool generatePath = true, bool forceDest = false)
	{
		MoveTo(new Vector3(x, y, z), generatePath, forceDest);
	}

	public void SetParabolic(float amplitude, float time_shift)
	{
		args.time_perc = time_shift;
		args.parabolic_amplitude = amplitude;
		args.vertical_acceleration = 0.0f;
		args.flags.EnableParabolic();
	}

	public void SetParabolicVerticalAcceleration(float vertical_acceleration, float time_shift)
	{
		args.time_perc = time_shift;
		args.parabolic_amplitude = 0.0f;
		args.vertical_acceleration = vertical_acceleration;
		args.flags.EnableParabolic();
	}

	public void SetAnimation(AnimTier anim)
	{
		args.time_perc = 0.0f;

		args.animTier = new AnimTierTransition
		{
			AnimTier = (byte)anim
		};

		args.flags.EnableAnimation();
	}

	public void SetFacing(Vector3 spot)
	{
		TransportPathTransform transform = new(unit, args.TransformForTransport);
		var finalSpot = transform.Calc(spot);
		args.facing.f = new Vector3(finalSpot.X, finalSpot.Y, finalSpot.Z);
		args.facing.type = MonsterMoveType.FacingSpot;
	}

	public void DisableTransportPathTransformations()
	{
		args.TransformForTransport = false;
	}

	public void SetSpellEffectExtraData(SpellEffectExtraData spellEffectExtraData)
	{
		args.spellEffectExtra = spellEffectExtraData;
	}

	public List<Vector3> Path()
	{
		return args.path;
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
		args.flags.SetUnsetFlag(SplineFlag.Backward);
	}
}

// Transforms coordinates from global to transport offsets
public class TransportPathTransform
{
    private readonly Unit _owner;
    private readonly bool _transformForTransport;

	public TransportPathTransform(Unit owner, bool transformForTransport)
	{
		_owner = owner;
		_transformForTransport = transformForTransport;
	}

	public Vector3 Calc(Vector3 input)
	{
		var pos = new Position(input);

		if (_transformForTransport)
		{
			var transport = _owner.DirectTransport;

			if (transport != null)
				transport.CalculatePassengerOffset(pos);
		}

		return pos;
	}
}