﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game.Movement;

public class IdleMovementGenerator : MovementGenerator
{
	public IdleMovementGenerator()
	{
		Mode = MovementGeneratorMode.Default;
		Priority = MovementGeneratorPriority.Normal;
		Flags = MovementGeneratorFlags.Initialized;
		BaseUnitState = 0;
	}

	public override void Initialize(Unit owner)
	{
		owner.StopMoving();
	}

	public override void Reset(Unit owner)
	{
		owner.StopMoving();
	}

	public override bool Update(Unit owner, uint diff)
	{
		return true;
	}

	public override void Deactivate(Unit owner) { }

	public override void Finalize(Unit owner, bool active, bool movementInform)
	{
		AddFlag(MovementGeneratorFlags.Finalized);
	}

	public override MovementGeneratorType GetMovementGeneratorType()
	{
		return MovementGeneratorType.Idle;
	}
}

public class RotateMovementGenerator : MovementGenerator
{
	readonly uint _id;
	readonly uint _maxDuration;
	readonly RotateDirection _direction;
	uint _duration;

	public RotateMovementGenerator(uint id, uint time, RotateDirection direction)
	{
		_id = id;
		_duration = time;
		_maxDuration = time;
		_direction = direction;

		Mode = MovementGeneratorMode.Default;
		Priority = MovementGeneratorPriority.Normal;
		Flags = MovementGeneratorFlags.InitializationPending;
		BaseUnitState = UnitState.Rotating;
	}

	public override void Initialize(Unit owner)
	{
		RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
		AddFlag(MovementGeneratorFlags.Initialized);

		owner.StopMoving();

		/*
		*  TODO: This code should be handled somewhere else, like MovementInform
		*
		*  if (owner->GetVictim())
		*      owner->SetInFront(owner->GetVictim());
		*
		*  owner->AttackStop();
		*/
	}

	public override void Reset(Unit owner)
	{
		RemoveFlag(MovementGeneratorFlags.Deactivated);
		Initialize(owner);
	}

	public override bool Update(Unit owner, uint diff)
	{
		if (owner == null)
			return false;

		var angle = owner.Location.Orientation;
		angle += diff * MathFunctions.TwoPi / _maxDuration * (_direction == RotateDirection.Left ? 1.0f : -1.0f);
		angle = Math.Clamp(angle, 0.0f, MathF.PI * 2);

		MoveSplineInit init = new(owner);
		init.MoveTo(owner.Location, false);

		if (!owner.GetTransGUID().IsEmpty)
			init.DisableTransportPathTransformations();

		init.SetFacing(angle);
		init.Launch();

		if (_duration > diff)
		{
			_duration -= diff;
		}
		else
		{
			AddFlag(MovementGeneratorFlags.InformEnabled);

			return false;
		}

		return true;
	}

	public override void Deactivate(Unit owner)
	{
		AddFlag(MovementGeneratorFlags.Deactivated);
	}

	public override void Finalize(Unit owner, bool active, bool movementInform)
	{
		AddFlag(MovementGeneratorFlags.Finalized);

		if (movementInform && owner.IsCreature)
			owner.AsCreature.AI.MovementInform(MovementGeneratorType.Rotate, _id);
	}

	public override MovementGeneratorType GetMovementGeneratorType()
	{
		return MovementGeneratorType.Rotate;
	}
}

public class DistractMovementGenerator : MovementGenerator
{
	readonly float _orientation;

	uint _timer;

	public DistractMovementGenerator(uint timer, float orientation)
	{
		_timer = timer;
		_orientation = orientation;

		Mode = MovementGeneratorMode.Default;
		Priority = MovementGeneratorPriority.Highest;
		Flags = MovementGeneratorFlags.InitializationPending;
		BaseUnitState = UnitState.Distracted;
	}

	public override void Initialize(Unit owner)
	{
		RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
		AddFlag(MovementGeneratorFlags.Initialized);

		// Distracted creatures stand up if not standing
		if (!owner.IsStandState)
			owner.SetStandState(UnitStandStateType.Stand);

		MoveSplineInit init = new(owner);
		init.MoveTo(owner.Location, false);

		if (!owner.GetTransGUID().IsEmpty)
			init.DisableTransportPathTransformations();

		init.SetFacing(_orientation);
		init.Launch();
	}

	public override void Reset(Unit owner)
	{
		RemoveFlag(MovementGeneratorFlags.Deactivated);
		Initialize(owner);
	}

	public override bool Update(Unit owner, uint diff)
	{
		if (owner == null)
			return false;

		if (diff > _timer)
		{
			AddFlag(MovementGeneratorFlags.InformEnabled);

			return false;
		}

		_timer -= diff;

		return true;
	}

	public override void Deactivate(Unit owner)
	{
		AddFlag(MovementGeneratorFlags.Deactivated);
	}

	public override void Finalize(Unit owner, bool active, bool movementInform)
	{
		AddFlag(MovementGeneratorFlags.Finalized);

		// TODO: This code should be handled somewhere else
		// If this is a creature, then return orientation to original position (for idle movement creatures)
		if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled) && owner.IsCreature)
		{
			var angle = owner.AsCreature.HomePosition.Orientation;
			owner.SetFacingTo(angle);
		}
	}

	public override MovementGeneratorType GetMovementGeneratorType()
	{
		return MovementGeneratorType.Distract;
	}
}

public class AssistanceDistractMovementGenerator : DistractMovementGenerator
{
	public AssistanceDistractMovementGenerator(uint timer, float orientation) : base(timer, orientation)
	{
		Priority = MovementGeneratorPriority.Normal;
	}

	public override void Finalize(Unit owner, bool active, bool movementInform)
	{
		owner.ClearUnitState(UnitState.Distracted);
		owner.AsCreature.ReactState = ReactStates.Aggressive;
	}

	public override MovementGeneratorType GetMovementGeneratorType()
	{
		return MovementGeneratorType.AssistanceDistract;
	}
}