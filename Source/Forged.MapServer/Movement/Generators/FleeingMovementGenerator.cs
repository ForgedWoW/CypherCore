// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Movement.Generators;

public class FleeingMovementGenerator<T> : MovementGeneratorMedium<T> where T : Unit
{
    public const float MAX_QUIET_DISTANCE = 43.0f;
    public const float MIN_QUIET_DISTANCE = 28.0f;
    private readonly ObjectGuid _fleeTargetGUID;
    private readonly TimeTracker _timer;
    private PathGenerator _path;

    public FleeingMovementGenerator(ObjectGuid fright)
    {
        _fleeTargetGUID = fright;
        _timer = new TimeTracker();

        Mode = MovementGeneratorMode.Default;
        Priority = MovementGeneratorPriority.Highest;
        Flags = MovementGeneratorFlags.InitializationPending;
        BaseUnitState = UnitState.Fleeing;
    }

    public override void DoDeactivate(T owner)
    {
        AddFlag(MovementGeneratorFlags.Deactivated);
        owner.ClearUnitState(UnitState.FleeingMove);
    }

    public override void DoFinalize(T owner, bool active, bool movementInform)
    {
        AddFlag(MovementGeneratorFlags.Finalized);

        if (active)
        {
            if (owner.IsPlayer)
            {
                owner.RemoveUnitFlag(UnitFlags.Fleeing);
                owner.ClearUnitState(UnitState.FleeingMove);
                owner.StopMoving();
            }
            else
            {
                owner.RemoveUnitFlag(UnitFlags.Fleeing);
                owner.ClearUnitState(UnitState.FleeingMove);

                if (owner.Victim != null)
                    owner.SetTarget(owner.Victim.GUID);
            }
        }
    }

    public override void DoInitialize(T owner)
    {
        RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);
        AddFlag(MovementGeneratorFlags.Initialized);

        if (owner == null || !owner.IsAlive)
            return;

        // TODO: UNIT_FIELD_FLAGS should not be handled by generators
        owner.SetUnitFlag(UnitFlags.Fleeing);
        _path = null;
        SetTargetLocation(owner);
    }

    public override void DoReset(T owner)
    {
        RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);
        DoInitialize(owner);
    }

    public override bool DoUpdate(T owner, uint diff)
    {
        if (owner == null || !owner.IsAlive)
            return false;

        if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
        {
            AddFlag(MovementGeneratorFlags.Interrupted);
            owner.StopMoving();
            _path = null;

            return true;
        }
        else
        {
            RemoveFlag(MovementGeneratorFlags.Interrupted);
        }

        _timer.Update(diff);

        if ((HasFlag(MovementGeneratorFlags.SpeedUpdatePending) && !owner.MoveSpline.Finalized()) || (_timer.Passed && owner.MoveSpline.Finalized()))
        {
            RemoveFlag(MovementGeneratorFlags.Transitory);
            SetTargetLocation(owner);
        }

        return true;
    }
    public override MovementGeneratorType GetMovementGeneratorType()
    {
        return MovementGeneratorType.Fleeing;
    }

    public override void UnitSpeedChanged()
    {
        AddFlag(MovementGeneratorFlags.SpeedUpdatePending);
    }

    private void GetPoint(T owner, Position position)
    {
        float casterDistance, casterAngle;
        var fleeTarget = Global.ObjAccessor.GetUnit(owner, _fleeTargetGUID);

        if (fleeTarget != null)
        {
            casterDistance = fleeTarget.GetDistance(owner);

            if (casterDistance > 0.2f)
                casterAngle = fleeTarget.Location.GetAbsoluteAngle(owner.Location);
            else
                casterAngle = RandomHelper.FRand(0.0f, 2.0f * MathF.PI);
        }
        else
        {
            casterDistance = 0.0f;
            casterAngle = RandomHelper.FRand(0.0f, 2.0f * MathF.PI);
        }

        float distance, angle;

        if (casterDistance < MIN_QUIET_DISTANCE)
        {
            distance = RandomHelper.FRand(0.4f, 1.3f) * (MIN_QUIET_DISTANCE - casterDistance);
            angle = casterAngle + RandomHelper.FRand(-MathF.PI / 8.0f, MathF.PI / 8.0f);
        }
        else if (casterDistance > MAX_QUIET_DISTANCE)
        {
            distance = RandomHelper.FRand(0.4f, 1.0f) * (MAX_QUIET_DISTANCE - MIN_QUIET_DISTANCE);
            angle = -casterAngle + RandomHelper.FRand(-MathF.PI / 4.0f, MathF.PI / 4.0f);
        }
        else // we are inside quiet range
        {
            distance = RandomHelper.FRand(0.6f, 1.2f) * (MAX_QUIET_DISTANCE - MIN_QUIET_DISTANCE);
            angle = RandomHelper.FRand(0.0f, 2.0f * MathF.PI);
        }

        owner.MovePositionToFirstCollision(position, distance, angle);
    }

    private void SetTargetLocation(T owner)
    {
        if (owner == null || !owner.IsAlive)
            return;

        if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
        {
            AddFlag(MovementGeneratorFlags.Interrupted);
            owner.StopMoving();
            _path = null;

            return;
        }

        Position destination = new(owner.Location);
        GetPoint(owner, destination);

        // Add LOS check for target point
        if (!owner.Location.IsWithinLOS(destination.X, destination.Y, destination.Z))
        {
            _timer.Reset(200);

            return;
        }

        if (_path == null)
        {
            _path = new PathGenerator(owner);
            _path.SetPathLengthLimit(30.0f);
        }

        var result = _path.CalculatePath(destination);

        if (!result || _path.GetPathType().HasFlag(PathType.NoPath) || _path.GetPathType().HasFlag(PathType.Shortcut) || _path.GetPathType().HasFlag(PathType.FarFromPoly))
        {
            _timer.Reset(100);

            return;
        }

        owner.AddUnitState(UnitState.FleeingMove);

        MoveSplineInit init = new(owner);
        init.MovebyPath(_path.GetPath());
        init.SetWalk(false);
        var traveltime = (uint)init.Launch();
        _timer.Reset(traveltime + RandomHelper.URand(800, 1500));
    }
}

public class TimedFleeingMovementGenerator : FleeingMovementGenerator<Creature>
{
    private readonly TimeTracker _totalFleeTime;

    public TimedFleeingMovementGenerator(ObjectGuid fright, uint time) : base(fright)
    {
        _totalFleeTime = new TimeTracker(time);
    }

    public override void Finalize(Unit owner, bool active, bool movementInform)
    {
        AddFlag(MovementGeneratorFlags.Finalized);

        if (!active)
            return;

        owner.RemoveUnitFlag(UnitFlags.Fleeing);
        var victim = owner.Victim;

        if (victim != null)
            if (owner.IsAlive)
            {
                owner.AttackStop();
                owner.AsCreature.AI.AttackStart(victim);
            }

        if (movementInform)
        {
            var ownerCreature = owner.AsCreature;
            var ai = ownerCreature?.AI;

            ai?.MovementInform(MovementGeneratorType.TimedFleeing, 0);
        }
    }

    public override MovementGeneratorType GetMovementGeneratorType()
    {
        return MovementGeneratorType.TimedFleeing;
    }

    public override bool Update(Unit owner, uint diff)
    {
        if (owner == null || !owner.IsAlive)
            return false;

        _totalFleeTime.Update(diff);

        if (_totalFleeTime.Passed)
            return false;

        return DoUpdate(owner.AsCreature, diff);
    }
}