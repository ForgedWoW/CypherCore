// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Movement.Generators;

public class FollowMovementGenerator : MovementGenerator
{
    private static readonly uint CHECK_INTERVAL = 100;
    private static readonly float FOLLOW_RANGE_TOLERANCE = 1.0f;
    private readonly float _range;
    private readonly TimeTracker _checkTimer;
    private readonly AbstractFollower _abstractFollower;
    private ChaseAngle _angle;
    private PathGenerator _path;
    private Position _lastTargetPosition;

    public FollowMovementGenerator(Unit target, float range, ChaseAngle angle)
    {
        _abstractFollower = new AbstractFollower(target);
        _range = range;
        _angle = angle;

        Mode = MovementGeneratorMode.Default;
        Priority = MovementGeneratorPriority.Normal;
        Flags = MovementGeneratorFlags.InitializationPending;
        BaseUnitState = UnitState.Follow;

        _checkTimer = new TimeTracker(CHECK_INTERVAL);
    }

    public override void Initialize(Unit owner)
    {
        RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
        AddFlag(MovementGeneratorFlags.Initialized | MovementGeneratorFlags.InformEnabled);

        owner.StopMoving();
        UpdatePetSpeed(owner);
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
        // owner might be dead or gone
        if (owner == null || !owner.IsAlive)
            return false;

        // our target might have gone away
        var target = _abstractFollower.GetTarget();

        if (target == null || !target.Location.IsInWorld)
            return false;

        if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
        {
            _path = null;
            owner.StopMoving();
            _lastTargetPosition = null;

            return true;
        }

        _checkTimer.Update(diff);

        if (_checkTimer.Passed)
        {
            _checkTimer.Reset(CHECK_INTERVAL);

            if (HasFlag(MovementGeneratorFlags.InformEnabled) && PositionOkay(owner, target, _range, _angle))
            {
                RemoveFlag(MovementGeneratorFlags.InformEnabled);
                _path = null;
                owner.StopMoving();
                _lastTargetPosition = new Position();
                DoMovementInform(owner, target);

                return true;
            }
        }

        if (owner.HasUnitState(UnitState.FollowMove) && owner.MoveSpline.Finalized())
        {
            RemoveFlag(MovementGeneratorFlags.InformEnabled);
            _path = null;
            owner.ClearUnitState(UnitState.FollowMove);
            DoMovementInform(owner, target);
        }

        if (_lastTargetPosition == null || _lastTargetPosition.GetExactDistSq(target.Location) > 0.0f)
        {
            _lastTargetPosition = new Position(target.Location);

            if (owner.HasUnitState(UnitState.FollowMove) || !PositionOkay(owner, target, _range + FOLLOW_RANGE_TOLERANCE))
            {
                if (_path == null)
                    _path = new PathGenerator(owner);


                // select angle
                float tAngle;
                var curAngle = target.Location.GetRelativeAngle(owner.Location);

                if (_angle.IsAngleOkay(curAngle))
                {
                    tAngle = curAngle;
                }
                else
                {
                    var diffUpper = Position.NormalizeOrientation(curAngle - _angle.UpperBound());
                    var diffLower = Position.NormalizeOrientation(_angle.LowerBound() - curAngle);

                    if (diffUpper < diffLower)
                        tAngle = _angle.UpperBound();
                    else
                        tAngle = _angle.LowerBound();
                }

                var newPos = new Position();
                target.Location.GetNearPoint(owner, newPos, _range, target.Location.ToAbsoluteAngle(tAngle));

                if (owner.IsHovering)
                    owner.Location.UpdateAllowedPositionZ(newPos);

                // pets are allowed to "cheat" on pathfinding when following their master
                var allowShortcut = false;
                var oPet = owner.AsPet;

                if (oPet != null)
                    if (target.GUID == oPet.OwnerGUID)
                        allowShortcut = true;

                var success = _path.CalculatePath(newPos, allowShortcut);

                if (!success || _path.GetPathType().HasFlag(PathType.NoPath))
                {
                    owner.StopMoving();

                    return true;
                }

                owner.AddUnitState(UnitState.FollowMove);
                AddFlag(MovementGeneratorFlags.InformEnabled);

                MoveSplineInit init = new(owner);
                init.MovebyPath(_path.GetPath());
                init.SetWalk(target.IsWalking);
                init.SetFacing(target.Location.Orientation);
                init.Launch();
            }
        }

        return true;
    }

    public override void Deactivate(Unit owner)
    {
        AddFlag(MovementGeneratorFlags.Deactivated);
        RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.InformEnabled);
        owner.ClearUnitState(UnitState.FollowMove);
    }

    public override void Finalize(Unit owner, bool active, bool movementInform)
    {
        AddFlag(MovementGeneratorFlags.Finalized);

        if (active)
        {
            owner.ClearUnitState(UnitState.FollowMove);
            UpdatePetSpeed(owner);
        }
    }

    public Unit GetTarget()
    {
        return _abstractFollower.GetTarget();
    }


    public override MovementGeneratorType GetMovementGeneratorType()
    {
        return MovementGeneratorType.Follow;
    }

    public override void UnitSpeedChanged()
    {
        _lastTargetPosition = null;
    }

    private void UpdatePetSpeed(Unit owner)
    {
        var oPet = owner.AsPet;

        if (oPet != null)
            if (!_abstractFollower.GetTarget() || _abstractFollower.GetTarget().GUID == owner.OwnerGUID)
            {
                oPet.UpdateSpeed(UnitMoveType.Run);
                oPet.UpdateSpeed(UnitMoveType.Walk);
                oPet.UpdateSpeed(UnitMoveType.Swim);
            }
    }

    private static bool PositionOkay(Unit owner, Unit target, float range, ChaseAngle? angle = null)
    {
        if (owner.Location.GetExactDistSq(target.Location) > (owner.CombatReach + target.CombatReach + range) * (owner.CombatReach + target.CombatReach + range))
            return false;

        return !angle.HasValue || angle.Value.IsAngleOkay(target.Location.GetRelativeAngle(owner.Location));
    }

    private static void DoMovementInform(Unit owner, Unit target)
    {
        if (!owner.IsCreature)
            return;

        var ai = owner.AsCreature.AI;

        if (ai != null)
            ai.MovementInform(MovementGeneratorType.Follow, (uint)target.GUID.Counter);
    }
}