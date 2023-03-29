// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Movement.Generators;

internal class ChaseMovementGenerator : MovementGenerator
{
    private static readonly uint RANGE_CHECK_INTERVAL = 100; // time (ms) until we attempt to recalculate
    private readonly TimeTracker _rangeCheckTimer;
    private readonly bool _movingTowards = true;
    private readonly AbstractFollower _abstractFollower;

    private readonly ChaseRange? _range;
    private readonly ChaseAngle? _angle;

    private PathGenerator _path;
    private Position _lastTargetPosition;
    private bool _mutualChase = true;

    public ChaseMovementGenerator(Unit target, ChaseRange? range, ChaseAngle? angle)
    {
        _abstractFollower = new AbstractFollower(target);
        _range = range;
        _angle = angle;

        Mode = MovementGeneratorMode.Default;
        Priority = MovementGeneratorPriority.Normal;
        Flags = MovementGeneratorFlags.InitializationPending;
        BaseUnitState = UnitState.Chase;

        _rangeCheckTimer = new TimeTracker(RANGE_CHECK_INTERVAL);
    }

    public override void Initialize(Unit owner)
    {
        RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
        AddFlag(MovementGeneratorFlags.Initialized | MovementGeneratorFlags.InformEnabled);

        _path = null;
        _lastTargetPosition = null;
    }

    public override void Reset(Unit owner)
    {
        RemoveFlag(MovementGeneratorFlags.Deactivated);
        Initialize(owner);
    }

    public override bool Update(Unit owner, uint diff)
    {
        // owner might be dead or gone (can we even get nullptr here?)
        if (!owner || !owner.IsAlive)
            return false;

        // our target might have gone away
        var target = _abstractFollower.GetTarget();

        if (target == null || !target.Location.IsInWorld)
            return false;

        // the owner might be unable to move (rooted or casting), or we have lost the target, pause movement
        if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting() || HasLostTarget(owner, target))
        {
            owner.StopMoving();
            _lastTargetPosition = null;
            var cOwner = owner.AsCreature;

            if (cOwner != null)
                cOwner.SetCannotReachTarget(false);

            return true;
        }

        var mutualChase = IsMutualChase(owner, target);
        var hitboxSum = owner.CombatReach + target.CombatReach;

        if (SharedConst.DefaultPlayerCombatReach > hitboxSum)
            hitboxSum = SharedConst.DefaultPlayerCombatReach;

        var minRange = _range.HasValue ? _range.Value.MinRange + hitboxSum : SharedConst.ContactDistance;
        var minTarget = (_range.HasValue ? _range.Value.MinTolerance : 0.0f) + hitboxSum;
        var maxRange = _range.HasValue ? _range.Value.MaxRange + hitboxSum : owner.GetMeleeRange(target); // melee range already includes hitboxes
        var maxTarget = _range.HasValue ? _range.Value.MaxTolerance + hitboxSum : SharedConst.ContactDistance + hitboxSum;
        var angle = mutualChase ? null : _angle;

        // periodically check if we're already in the expected range...
        _rangeCheckTimer.Update(diff);

        if (_rangeCheckTimer.Passed)
        {
            _rangeCheckTimer.Reset(RANGE_CHECK_INTERVAL);

            if (HasFlag(MovementGeneratorFlags.InformEnabled) && PositionOkay(owner, target, _movingTowards ? null : minTarget, _movingTowards ? maxTarget : null, angle))
            {
                RemoveFlag(MovementGeneratorFlags.InformEnabled);
                _path = null;

                var cOwner = owner.AsCreature;

                if (cOwner != null)
                    cOwner.SetCannotReachTarget(false);

                owner.StopMoving();
                owner.SetInFront(target);
                DoMovementInform(owner, target);

                return true;
            }
        }

        var isEvading = false;

        // if we're done moving, we want to clean up
        if (owner.HasUnitState(UnitState.ChaseMove) && owner.MoveSpline.Finalized())
        {
            RemoveFlag(MovementGeneratorFlags.InformEnabled);
            _path = null;
            var cOwner = owner.AsCreature;

            if (cOwner != null)
                cOwner.SetCannotReachTarget(false);

            owner.ClearUnitState(UnitState.ChaseMove);
            owner.SetInFront(target);
            DoMovementInform(owner, target);
        }

        // if the target moved, we have to consider whether to adjust
        if (_lastTargetPosition == null || target.Location != _lastTargetPosition || mutualChase != _mutualChase)
        {
            _lastTargetPosition = new Position(target.Location);
            _mutualChase = mutualChase;

            if (owner.HasUnitState(UnitState.ChaseMove) || !PositionOkay(owner, target, minRange, maxRange, angle))
            {
                var cOwner = owner.AsCreature;

                // can we get to the target?
                if (cOwner != null && !target.IsInAccessiblePlaceFor(cOwner))
                {
                    cOwner.SetCannotReachTarget(true);
                    cOwner.StopMoving();
                    _path = null;

                    return true;
                }

                // figure out which way we want to move
                var moveToward = !owner.Location.IsInDist(target.Location, maxRange);

                // make a new path if we have to...
                if (_path == null || moveToward != _movingTowards)
                    _path = new PathGenerator(owner);

                var pos = new Position();
                bool shortenPath;

                // if we want to move toward the target and there's no fixed angle...
                if (moveToward && !angle.HasValue)
                {
                    // ...we'll pathfind to the center, then shorten the path
                    pos = target.Location.Copy();
                    shortenPath = true;
                }
                else
                {
                    // otherwise, we fall back to nearpoint finding
                    target.Location.GetNearPoint(owner, pos, (moveToward ? maxTarget : minTarget) - hitboxSum, angle.HasValue ? target.Location.ToAbsoluteAngle(angle.Value.RelativeAngle) : target.Location.GetAbsoluteAngle(owner.Location));
                    shortenPath = false;
                }

                if (owner.IsHovering)
                    owner.Location.UpdateAllowedPositionZ(pos);

                var success = _path.CalculatePath(pos, owner.CanFly);

                if (!success || _path.GetPathType().HasAnyFlag(PathType.NoPath))
                {
                    if (cOwner)
                        cOwner.SetCannotReachTarget(true);

                    owner.StopMoving();

                    return true;
                }

                if (shortenPath)
                    _path.ShortenPathUntilDist(target.Location, maxTarget);

                if (cOwner)
                    cOwner.SetCannotReachTarget(false);

                cOwner.SetCannotReachTarget(false);

                var walk = false;

                if (cOwner && !cOwner.IsPet)
                    switch (cOwner.MovementTemplate.GetChase())
                    {
                        case CreatureChaseMovementType.CanWalk:
                            walk = owner.IsWalking;

                            break;
                        case CreatureChaseMovementType.AlwaysWalk:
                            walk = true;

                            break;
                        default:
                            break;
                    }

                owner.AddUnitState(UnitState.ChaseMove);
                AddFlag(MovementGeneratorFlags.InformEnabled);

                MoveSplineInit init = new(owner);
                init.MovebyPath(_path.GetPath());
                init.SetWalk(walk);
                init.SetFacing(target);
                init.Launch();
            }
        }

        // and then, finally, we're done for the tick
        return true;
    }

    public override void Deactivate(Unit owner)
    {
        AddFlag(MovementGeneratorFlags.Deactivated);
        RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.InformEnabled);
        owner.ClearUnitState(UnitState.ChaseMove);
        var cOwner = owner.AsCreature;

        if (cOwner != null)
            cOwner.SetCannotReachTarget(false);
    }

    public override void Finalize(Unit owner, bool active, bool movementInform)
    {
        AddFlag(MovementGeneratorFlags.Finalized);

        if (active)
        {
            owner.ClearUnitState(UnitState.ChaseMove);
            var cOwner = owner.AsCreature;

            if (cOwner != null)
                cOwner.SetCannotReachTarget(false);
        }
    }

    public override MovementGeneratorType GetMovementGeneratorType()
    {
        return MovementGeneratorType.Chase;
    }

    public override void UnitSpeedChanged()
    {
        _lastTargetPosition = null;
    }

    public Unit GetTarget()
    {
        return _abstractFollower.GetTarget();
    }

    private static bool HasLostTarget(Unit owner, Unit target)
    {
        return owner.Victim != target;
    }

    private static bool IsMutualChase(Unit owner, Unit target)
    {
        if (target.MotionMaster.GetCurrentMovementGeneratorType() != MovementGeneratorType.Chase)
            return false;

        var movement = target.MotionMaster.GetCurrentMovementGenerator() as ChaseMovementGenerator;

        if (movement != null)
            return movement.GetTarget() == owner;

        return false;
    }

    private static bool PositionOkay(Unit owner, Unit target, float? minDistance, float? maxDistance, ChaseAngle? angle)
    {
        var distSq = owner.Location.GetExactDistSq(target.Location);

        if (minDistance.HasValue && distSq < minDistance.Value * minDistance.Value)
            return false;

        if (maxDistance.HasValue && distSq > maxDistance.Value * maxDistance.Value)
            return false;

        if (angle.HasValue && !angle.Value.IsAngleOkay(target.Location.GetRelativeAngle(owner.Location)))
            return false;

        if (!owner.Location.IsWithinLOSInMap(target))
            return false;

        return true;
    }

    private static void DoMovementInform(Unit owner, Unit target)
    {
        if (!owner.IsCreature)
            return;

        var ai = owner.AsCreature.AI;

        if (ai != null)
            ai.MovementInform(MovementGeneratorType.Chase, (uint)target.GUID.Counter);
    }
}