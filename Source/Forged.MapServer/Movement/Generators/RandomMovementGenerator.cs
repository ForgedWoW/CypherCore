﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Movement.Generators;

public class RandomMovementGenerator : MovementGeneratorMedium<Creature>
{
    private readonly TimeTracker _timer;

    private PathGenerator _path;
    private Position _reference;
    private float _wanderDistance;
    private uint _wanderSteps;

    public RandomMovementGenerator(float spawnDist = 0.0f, TimeSpan duration = default)
    {
        _timer = new TimeTracker(duration);
        _reference = new Position();
        _wanderDistance = spawnDist;

        Mode = MovementGeneratorMode.Default;
        Priority = MovementGeneratorPriority.Normal;
        Flags = MovementGeneratorFlags.InitializationPending;
        BaseUnitState = UnitState.Roaming;
    }

    public override void DoDeactivate(Creature owner)
    {
        AddFlag(MovementGeneratorFlags.Deactivated);
        owner.ClearUnitState(UnitState.RoamingMove);
    }

    public override void DoFinalize(Creature owner, bool active, bool movementInform)
    {
        AddFlag(MovementGeneratorFlags.Finalized);

        if (active)
        {
            owner.ClearUnitState(UnitState.RoamingMove);
            owner.StopMoving();

            // TODO: Research if this modification is needed, which most likely isnt
            owner.SetWalk(false);
        }

        if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled) && owner.IsAIEnabled && owner.TryGetCreatureAI(out var ai))
            ai.MovementInform(MovementGeneratorType.Random, 0);
    }

    public override void DoInitialize(Creature owner)
    {
        RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated | MovementGeneratorFlags.Paused);
        AddFlag(MovementGeneratorFlags.Initialized);

        if (owner == null || !owner.IsAlive)
            return;

        _reference = owner.Location;
        owner.StopMoving();

        if (_wanderDistance == 0f)
            _wanderDistance = owner.WanderDistance;

        // Retail seems to let a creature walk 2 up to 10 splines before triggering a pause
        _wanderSteps = RandomHelper.URand(2, 10);

        _timer.Reset(0);
        _path = null;
    }

    public override void DoReset(Creature owner)
    {
        RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);
        DoInitialize(owner);
    }

    public override bool DoUpdate(Creature owner, uint diff)
    {
        if (!owner || !owner.IsAlive)
            return true;

        if (HasFlag(MovementGeneratorFlags.Finalized | MovementGeneratorFlags.Paused))
            return true;

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

        lock (_reference)
        {
            _timer.Update(diff);

            if ((HasFlag(MovementGeneratorFlags.SpeedUpdatePending) && !owner.MoveSpline.Finalized()) || (_timer.Passed && owner.MoveSpline.Finalized()))
                SetRandomLocation(owner);
        }

        if (_timer.Passed)
        {
            RemoveFlag(MovementGeneratorFlags.Transitory);
            AddFlag(MovementGeneratorFlags.InformEnabled);

            return false;
        }

        return true;
    }
    public override MovementGeneratorType GetMovementGeneratorType()
    {
        return MovementGeneratorType.Random;
    }

    public override void Pause(uint timer = 0)
    {
        if (timer != 0)
        {
            AddFlag(MovementGeneratorFlags.TimedPaused);
            _timer.Reset(timer);
            RemoveFlag(MovementGeneratorFlags.Paused);
        }
        else
        {
            AddFlag(MovementGeneratorFlags.Paused);
            RemoveFlag(MovementGeneratorFlags.TimedPaused);
        }
    }

    public override void Resume(uint overrideTimer = 0)
    {
        if (overrideTimer != 0)
            _timer.Reset(overrideTimer);

        RemoveFlag(MovementGeneratorFlags.Paused);
    }

    public override void UnitSpeedChanged()
    {
        AddFlag(MovementGeneratorFlags.SpeedUpdatePending);
    }
    private void SetRandomLocation(Creature owner)
    {
        if (owner == null)
            return;

        if (owner.HasUnitState(UnitState.NotMove | UnitState.LostControl) || owner.IsMovementPreventedByCasting())
        {
            AddFlag(MovementGeneratorFlags.Interrupted);
            owner.StopMoving();
            _path = null;

            return;
        }

        Position position = new(_reference);
        var distance = RandomHelper.FRand(0.0f, _wanderDistance);
        var angle = RandomHelper.FRand(0.0f, MathF.PI * 2.0f);
        owner.MovePositionToFirstCollision(position, distance, angle);

        // Check if the destination is in LOS
        if (!owner.Location.IsWithinLOS(position.X, position.Y, position.Z))
        {
            // Retry later on
            _timer.Reset(200);

            return;
        }

        if (_path == null)
        {
            _path = new PathGenerator(owner);
            _path.SetPathLengthLimit(30.0f);
        }

        var result = _path.CalculatePath(position);

        // PATHFIND_FARFROMPOLY shouldn't be checked as creatures in water are most likely far from poly
        if (!result || _path.GetPathType().HasFlag(PathType.NoPath) || _path.GetPathType().HasFlag(PathType.Shortcut)) // || _path.GetPathType().HasFlag(PathType.FarFromPoly))
        {
            _timer.Reset(100);

            return;
        }

        RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.TimedPaused);

        owner.AddUnitState(UnitState.RoamingMove);

        var walk = true;

        switch (owner.MovementTemplate.GetRandom())
        {
            case CreatureRandomMovementType.CanRun:
                walk = owner.IsWalking;

                break;
            case CreatureRandomMovementType.AlwaysRun:
                walk = false;

                break;
            default:
                break;
        }

        MoveSplineInit init = new(owner);
        init.MovebyPath(_path.GetPath());
        init.SetWalk(walk);
        var splineDuration = (uint)init.Launch();

        --_wanderSteps;

        if (_wanderSteps != 0) // Creature has yet to do steps before pausing
        {
            _timer.Reset(splineDuration);
        }
        else
        {
            // Creature has made all its steps, time for a little break
            _timer.Reset(splineDuration + RandomHelper.URand(4, 10) * Time.IN_MILLISECONDS); // Retails seems to use rounded numbers so we do as well
            _wanderSteps = RandomHelper.URand(2, 10);
        }

        // Call for creature group update
        owner.SignalFormationMovement();
    }
}