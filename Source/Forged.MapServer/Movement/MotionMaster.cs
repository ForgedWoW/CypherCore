// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Forged.MapServer.AI;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Movement.Generators;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Movement;

public class MotionMaster
{
    public const double GRAVITY = 19.29110527038574;
    public const float SPEED_CHARGE = 42.0f;
    private static readonly IdleMovementGenerator StaticIdleMovement = new();
    private static uint _splineId;

    public static uint SplineId => _splineId++;

    private MultiMap<uint, MovementGenerator> BaseUnitStatesMap { get; } = new();
    private MovementGenerator DefaultGenerator { get; set; }
    private ConcurrentQueue<DelayedAction> DelayedActions { get; } = new();
    private MotionMasterFlags Flags { get; set; }
    private SortedSet<MovementGenerator> Generators { get; } = new(new MovementGeneratorComparator());
    private Unit Owner { get; }

    public MotionMaster(Unit unit)
    {
        Owner = unit;
        Flags = MotionMasterFlags.InitializationPending;
    }

    public static MovementGenerator GetIdleMovementGenerator()
    {
        return StaticIdleMovement;
    }

    public static bool IsInvalidMovementGeneratorType(MovementGeneratorType type)
    {
        return type == MovementGeneratorType.MaxDB || type >= MovementGeneratorType.Max;
    }

    public static bool IsInvalidMovementSlot(MovementSlot slot)
    {
        return slot >= MovementSlot.Max;
    }

    public static bool IsStatic(MovementGenerator movement)
    {
        return (movement == GetIdleMovementGenerator());
    }

    public void AddToWorld()
    {
        if (!HasFlag(MotionMasterFlags.InitializationPending))
            return;

        AddFlag(MotionMasterFlags.Initializing);
        RemoveFlag(MotionMasterFlags.InitializationPending);

        DirectInitialize();
        ResolveDelayedActions();

        RemoveFlag(MotionMasterFlags.Initializing);
    }

    public void Clear()
    {
        if (HasFlag(MotionMasterFlags.Delayed))
        {
            DelayedActions.Enqueue(new DelayedAction(Clear, MotionMasterDelayedActionType.Clear));

            return;
        }

        if (!Empty())
            DirectClear();
    }

    public void Clear(MovementSlot slot)
    {
        if (IsInvalidMovementSlot(slot))
            return;

        if (HasFlag(MotionMasterFlags.Delayed))
        {
            DelayedActions.Enqueue(new DelayedAction(() => Clear(slot), MotionMasterDelayedActionType.ClearSlot));

            return;
        }

        if (Empty())
            return;

        switch (slot)
        {
            case MovementSlot.Default:
                DirectClearDefault();

                break;

            case MovementSlot.Active:
                DirectClear();

                break;

            default:
                break;
        }
    }

    public void Clear(MovementGeneratorMode mode)
    {
        if (HasFlag(MotionMasterFlags.Delayed))
        {
            DelayedActions.Enqueue(new DelayedAction(() => Clear(mode), MotionMasterDelayedActionType.ClearMode));

            return;
        }

        if (Empty())
            return;

        DirectClear(a => a.Mode == mode);
    }

    public void Clear(MovementGeneratorPriority priority)
    {
        if (HasFlag(MotionMasterFlags.Delayed))
        {
            DelayedActions.Enqueue(new DelayedAction(() => Clear(priority), MotionMasterDelayedActionType.ClearPriority));

            return;
        }

        if (Empty())
            return;

        DirectClear(a => a.Priority == priority);
    }

    public bool Empty()
    {
        lock (Generators)
        {
            return DefaultGenerator == null && Generators.Empty();
        }
    }

    public MovementGenerator GetCurrentMovementGenerator()
    {
        lock (Generators)
        {
            if (!Generators.Empty())
                return Generators.FirstOrDefault();
        }

        return DefaultGenerator;
    }

    public MovementGenerator GetCurrentMovementGenerator(MovementSlot slot)
    {
        if (Empty() || IsInvalidMovementSlot(slot))
            return null;

        lock (Generators)
        {
            if (slot == MovementSlot.Active && !Generators.Empty())
                return Generators.FirstOrDefault();
        }

        if (slot == MovementSlot.Default && DefaultGenerator != null)
            return DefaultGenerator;

        return null;
    }

    public MovementGeneratorType GetCurrentMovementGeneratorType()
    {
        if (Empty())
            return MovementGeneratorType.Max;

        var movement = GetCurrentMovementGenerator();

        if (movement == null)
            return MovementGeneratorType.Max;

        return movement.GetMovementGeneratorType();
    }

    public MovementGeneratorType GetCurrentMovementGeneratorType(MovementSlot slot)
    {
        if (Empty() || IsInvalidMovementSlot(slot))
            return MovementGeneratorType.Max;

        lock (Generators)
        {
            if (slot == MovementSlot.Active && !Generators.Empty())
                return Generators.FirstOrDefault().GetMovementGeneratorType();
        }

        if (slot == MovementSlot.Default && DefaultGenerator != null)
            return DefaultGenerator.GetMovementGeneratorType();

        return MovementGeneratorType.Max;
    }

    public MovementSlot GetCurrentSlot()
    {
        lock (Generators)
        {
            if (!Generators.Empty())
                return MovementSlot.Active;
        }

        if (DefaultGenerator != null)
            return MovementSlot.Default;

        return MovementSlot.Max;
    }

    public bool GetDestination(out float x, out float y, out float z)
    {
        x = 0f;
        y = 0f;
        z = 0f;

        if (Owner.MoveSpline.Finalized())
            return false;

        var dest = Owner.MoveSpline.FinalDestination();
        x = dest.X;
        y = dest.Y;
        z = dest.Z;

        return true;
    }

    public MovementGenerator GetMovementGenerator(Func<MovementGenerator, bool> filter, MovementSlot slot = MovementSlot.Active)
    {
        if (Empty() || IsInvalidMovementSlot(slot))
            return null;

        MovementGenerator movement = null;

        switch (slot)
        {
            case MovementSlot.Default:
                if (DefaultGenerator != null && filter(DefaultGenerator))
                    movement = DefaultGenerator;

                break;

            case MovementSlot.Active:
                lock (Generators)
                {
                    if (!Generators.Empty())
                    {
                        var itr = Generators.FirstOrDefault(filter);

                        if (itr != null)
                            movement = itr;
                    }
                }

                break;

            default:
                break;
        }

        return movement;
    }

    public List<MovementGeneratorInformation> GetMovementGeneratorsInformation()
    {
        List<MovementGeneratorInformation> list = new();

        if (DefaultGenerator != null)
            list.Add(new MovementGeneratorInformation(DefaultGenerator.GetMovementGeneratorType(), ObjectGuid.Empty));

        lock (Generators)
        {
            foreach (var movement in Generators)
            {
                var type = movement.GetMovementGeneratorType();

                switch (type)
                {
                    case MovementGeneratorType.Chase:
                    case MovementGeneratorType.Follow:
                        var followInformation = movement as FollowMovementGenerator;

                        if (followInformation != null)
                        {
                            var target = followInformation.GetTarget();

                            if (target != null)
                                list.Add(new MovementGeneratorInformation(type, target.GUID, target.GetName()));
                            else
                                list.Add(new MovementGeneratorInformation(type, ObjectGuid.Empty));
                        }
                        else
                        {
                            list.Add(new MovementGeneratorInformation(type, ObjectGuid.Empty));
                        }

                        break;

                    default:
                        list.Add(new MovementGeneratorInformation(type, ObjectGuid.Empty));

                        break;
                }
            }
        }

        return list;
    }

    public bool HasMovementGenerator(Func<MovementGenerator, bool> filter, MovementSlot slot = MovementSlot.Active)
    {
        if (Empty() || IsInvalidMovementSlot(slot))
            return false;

        var value = false;

        switch (slot)
        {
            case MovementSlot.Default:
                if (DefaultGenerator != null && filter(DefaultGenerator))
                    value = true;

                break;

            case MovementSlot.Active:
                lock (Generators)
                {
                    if (!Generators.Empty())
                    {
                        var itr = Generators.FirstOrDefault(filter);
                        value = itr != null;
                    }
                }

                break;

            default:
                break;
        }

        return value;
    }

    public void Initialize()
    {
        if (HasFlag(MotionMasterFlags.InitializationPending))
            return;

        if (HasFlag(MotionMasterFlags.Update))
        {
            DelayedActions.Enqueue(new DelayedAction(Initialize, MotionMasterDelayedActionType.Initialize));

            return;
        }

        DirectInitialize();
    }

    public void InitializeDefault()
    {
        Add(AISelector.SelectMovementGenerator(Owner), MovementSlot.Default);
    }

    public void LaunchMoveSpline(Action<MoveSplineInit> initializer, uint id = 0, MovementGeneratorPriority priority = MovementGeneratorPriority.Normal, MovementGeneratorType type = MovementGeneratorType.Effect)
    {
        if (IsInvalidMovementGeneratorType(type))
        {
            Log.Logger.Debug($"MotionMaster::LaunchMoveSpline: '{Owner.GUID}', tried to launch a spline with an invalid MovementGeneratorType: {type} (Id: {id}, Priority: {priority})");

            return;
        }

        GenericMovementGenerator movement = new(initializer, type, id)
        {
            Priority = priority
        };

        Add(movement);
    }

    public void MoveAlongSplineChain(uint pointId, uint dbChainId, bool walk)
    {
        var owner = Owner.AsCreature;

        if (!owner)
        {
            Log.Logger.Error("MotionMaster.MoveAlongSplineChain: non-creature {0} tried to walk along DB spline chain. Ignoring.", Owner.GUID.ToString());

            return;
        }

        var chain = Global.ScriptMgr.GetSplineChain(owner, (byte)dbChainId);

        if (chain.Empty())
        {
            Log.Logger.Error("MotionMaster.MoveAlongSplineChain: creature with entry {0} tried to walk along non-existing spline chain with DB id {1}.", owner.Entry, dbChainId);

            return;
        }

        MoveAlongSplineChain(pointId, chain, walk);
    }

    public void MoveCharge(float x, float y, float z, float speed = SPEED_CHARGE, uint id = EventId.Charge, bool generatePath = false, Unit target = null, SpellEffectExtraData spellEffectExtraData = null)
    {
        /*
        if (_slot[(int)MovementSlot.Controlled] != null && _slot[(int)MovementSlot.Controlled].GetMovementGeneratorType() != MovementGeneratorType.Distract)
            return;
        */

        PointMovementGenerator movement = new(id, x, y, z, generatePath, speed, null, target, spellEffectExtraData)
        {
            Priority = MovementGeneratorPriority.Highest,
            BaseUnitState = UnitState.Charging
        };

        Add(movement);
    }

    public void MoveCharge(PathGenerator path, float speed = SPEED_CHARGE, Unit target = null, SpellEffectExtraData spellEffectExtraData = null)
    {
        var dest = path.GetActualEndPosition();

        MoveCharge(dest.X, dest.Y, dest.Z, SPEED_CHARGE, EventId.ChargePrepath);

        // Charge movement is not started when using EVENT_CHARGE_PREPATH
        MoveSplineInit init = new(Owner);
        init.MovebyPath(path.GetPath());
        init.SetVelocity(speed);

        if (target != null)
            init.SetFacing(target);

        if (spellEffectExtraData != null)
            init.SetSpellEffectExtraData(spellEffectExtraData);

        init.Launch();
    }

    public void MoveChase(Unit target, float dist, float angle = 0.0f)
    {
        MoveChase(target, new ChaseRange(dist), new ChaseAngle(angle));
    }

    public void MoveChase(Unit target, float dist)
    {
        MoveChase(target, new ChaseRange(dist));
    }

    public void MoveChase(Unit target, ChaseRange? dist = null, ChaseAngle? angle = null)
    {
        // Ignore movement request if target not exist
        if (!target || target == Owner)
            return;

        Add(new ChaseMovementGenerator(target, dist, angle));
    }

    public void MoveCirclePath(float x, float y, float z, float radius, bool clockwise, byte stepCount)
    {
        var initializer = (MoveSplineInit init) =>
        {
            var step = 2 * MathFunctions.PI / stepCount * (clockwise ? -1.0f : 1.0f);
            Position pos = new(x, y, z);
            var angle = pos.GetAbsoluteAngle(Owner.Location.X, Owner.Location.Y);

            // add the owner's current position as starting point as it gets removed after entering the cycle
            init.Path().Add(new Vector3(Owner.Location.X, Owner.Location.Y, Owner.Location.Z));

            for (byte i = 0; i < stepCount; angle += step, ++i)
            {
                Vector3 point = new()
                {
                    X = (float)(x + radius * Math.Cos(angle)),
                    Y = (float)(y + radius * Math.Sin(angle))
                };

                if (Owner.IsFlying)
                    point.Z = z;
                else
                    point.Z = Owner.Location.GetMapHeight(point.X, point.Y, z) + Owner.HoverOffset;

                init.Path().Add(point);
            }

            if (Owner.IsFlying)
            {
                init.SetFly();
                init.SetCyclic();
                init.SetAnimation(AnimTier.Hover);
            }
            else
            {
                init.SetWalk(true);
                init.SetCyclic();
            }
        };

        Add(new GenericMovementGenerator(initializer, MovementGeneratorType.Effect, 0));
    }

    public void MoveCloserAndStop(uint id, Unit target, float distance)
    {
        var distanceToTravel = Owner.Location.GetExactDist2d(target.Location) - distance;

        if (distanceToTravel > 0.0f)
        {
            var angle = Owner.Location.GetAbsoluteAngle(target.Location);
            var destx = Owner.Location.X + distanceToTravel * (float)Math.Cos(angle);
            var desty = Owner.Location.Y + distanceToTravel * (float)Math.Sin(angle);
            MovePoint(id, destx, desty, target.Location.Z);
        }
        else
        {
            // We are already close enough. We just need to turn toward the target without changing position.
            var initializer = (MoveSplineInit init) =>
            {
                init.MoveTo(Owner.Location.X, Owner.Location.Y, Owner.Location.Z);
                var refreshedTarget = Global.ObjAccessor.GetUnit(Owner, target.GUID);

                if (refreshedTarget != null)
                    init.SetFacing(refreshedTarget);
            };

            Add(new GenericMovementGenerator(initializer, MovementGeneratorType.Effect, id));
        }
    }

    public void MoveConfused()
    {
        if (Owner.IsTypeId(TypeId.Player))
            Add(new ConfusedMovementGenerator<Player>());
        else
            Add(new ConfusedMovementGenerator<Creature>());
    }

    public void MoveDistract(uint timer, float orientation)
    {
        /*
        if (_slot[(int)MovementSlot.Controlled] != null)
            return;
        */

        Add(new DistractMovementGenerator(timer, orientation));
    }

    public void MoveFall(uint id = 0)
    {
        // Use larger distance for vmap height search than in most other cases
        var tz = Owner.Location.GetMapHeight(Owner.Location.X, Owner.Location.Y, Owner.Location.Z, true, MapConst.MaxFallDistance);

        if (tz <= MapConst.InvalidHeight)
            return;

        // Abort too if the ground is very near
        if (Math.Abs(Owner.Location.Z - tz) < 0.1f)
            return;

        // rooted units don't move (also setting falling+root flag causes client freezes)
        if (Owner.HasUnitState(UnitState.Root | UnitState.Stunned))
            return;

        Owner.SetFall(true);

        // Don't run spline movement for players
        if (Owner.IsTypeId(TypeId.Player))
        {
            Owner.AsPlayer.SetFallInformation(0, Owner.Location.Z);

            return;
        }

        var initializer = (MoveSplineInit init) =>
        {
            init.MoveTo(Owner.Location.X, Owner.Location.Y, tz + Owner.HoverOffset, false);
            init.SetFall();
        };

        GenericMovementGenerator movement = new(initializer, MovementGeneratorType.Effect, id)
        {
            Priority = MovementGeneratorPriority.Highest
        };

        Add(movement);
    }

    public void MoveFleeing(Unit enemy, uint time)
    {
        if (!enemy)
            return;

        if (Owner.IsCreature)
        {
            if (time != 0)
                Add(new TimedFleeingMovementGenerator(enemy.GUID, time));
            else
                Add(new FleeingMovementGenerator<Creature>(enemy.GUID));
        }
        else
        {
            Add(new FleeingMovementGenerator<Player>(enemy.GUID));
        }
    }

    public void MoveFollow(Unit target, float dist, float angle = 0.0f, MovementSlot slot = MovementSlot.Active)
    {
        MoveFollow(target, dist, new ChaseAngle(angle), slot);
    }

    public void MoveFollow(Unit target, float dist, ChaseAngle angle, MovementSlot slot = MovementSlot.Active)
    {
        // Ignore movement request if target not exist
        if (!target || target == Owner)
            return;

        Add(new FollowMovementGenerator(target, dist, angle), slot);
    }

    public void MoveFormation(Unit leader, float range, float angle, uint point1, uint point2)
    {
        if (Owner.TypeId == TypeId.Unit && leader != null)
            Add(new FormationMovementGenerator(leader, range, angle, point1, point2), MovementSlot.Default);
    }

    public void MoveIdle()
    {
        Add(GetIdleMovementGenerator(), MovementSlot.Default);
    }

    public void MoveJump(Position pos, float speedXy, float speedZ, uint id = EventId.Jump, bool hasOrientation = false, JumpArrivalCastArgs arrivalCast = null, SpellEffectExtraData spellEffectExtraData = null)
    {
        MoveJump(pos.X, pos.Y, pos.Z, pos.Orientation, speedXy, speedZ, id, hasOrientation, arrivalCast, spellEffectExtraData);
    }

    public void MoveJump(float x, float y, float z, float speedXy, float speedZ, uint id = EventId.Jump, bool hasOrientation = false, JumpArrivalCastArgs arrivalCast = null, SpellEffectExtraData spellEffectExtraData = null)
    {
        MoveJump(x, y, z, 0, speedXy, speedZ, id, hasOrientation, arrivalCast, spellEffectExtraData);
    }

    public void MoveJump(float x, float y, float z, float o, float speedXy, float speedZ, uint id = EventId.Jump, bool hasOrientation = false, JumpArrivalCastArgs arrivalCast = null, SpellEffectExtraData spellEffectExtraData = null)
    {
        Log.Logger.Debug("Unit ({0}) jump to point (X: {1} Y: {2} Z: {3})", Owner.GUID.ToString(), x, y, z);

        if (speedXy < 0.01f)
            return;

        var moveTimeHalf = (float)(speedZ / GRAVITY);
        var maxHeight = -MoveSpline.ComputeFallElevation(moveTimeHalf, false, -speedZ);

        var initializer = (MoveSplineInit init) =>
        {
            init.MoveTo(x, y, z, false);
            init.SetParabolic(maxHeight, 0);
            init.SetVelocity(speedXy);

            if (hasOrientation)
                init.SetFacing(o);

            if (spellEffectExtraData != null)
                init.SetSpellEffectExtraData(spellEffectExtraData);
        };

        uint arrivalSpellId = 0;
        var arrivalSpellTargetGuid = ObjectGuid.Empty;

        if (arrivalCast != null)
        {
            arrivalSpellId = arrivalCast.SpellId;
            arrivalSpellTargetGuid = arrivalCast.Target;
        }

        GenericMovementGenerator movement = new(initializer, MovementGeneratorType.Effect, id, arrivalSpellId, arrivalSpellTargetGuid)
        {
            Priority = MovementGeneratorPriority.Highest,
            BaseUnitState = UnitState.Jumping
        };

        Add(movement);
    }

    public void MoveJumpTo(float angle, float speedXy, float speedZ)
    {
        //This function may make players fall below map
        if (Owner.IsTypeId(TypeId.Player))
            return;

        var moveTimeHalf = (float)(speedZ / GRAVITY);
        var dist = 2 * moveTimeHalf * speedXy;
        Owner.Location.GetNearPoint2D(null, out var x, out var y, dist, Owner.Location.Orientation + angle);
        var z = Owner.Location.Z;
        z = Owner.Location.UpdateAllowedPositionZ(x, y, z);
        MoveJump(x, y, z, 0.0f, speedXy, speedZ);
    }

    public void MoveJumpWithGravity(Position pos, float speedXy, float gravity, uint id = EventId.Jump, bool hasOrientation = false, JumpArrivalCastArgs arrivalCast = null, SpellEffectExtraData spellEffectExtraData = null)
    {
        Log.Logger.Debug($"MotionMaster.MoveJumpWithGravity: '{Owner.GUID}', jumps to point Id: {id} ({pos})");

        if (speedXy < 0.01f)
            return;

        var initializer = (MoveSplineInit init) =>
        {
            init.MoveTo(pos.X, pos.Y, pos.Z, false);
            init.SetParabolicVerticalAcceleration(gravity, 0);
            init.SetUncompressed();
            init.SetVelocity(speedXy);
            init.SetUnlimitedSpeed();

            if (hasOrientation)
                init.SetFacing(pos.Orientation);

            if (spellEffectExtraData != null)
                init.SetSpellEffectExtraData(spellEffectExtraData);
        };

        uint arrivalSpellId = 0;
        ObjectGuid arrivalSpellTargetGuid = default;

        if (arrivalCast != null)
        {
            arrivalSpellId = arrivalCast.SpellId;
            arrivalSpellTargetGuid = arrivalCast.Target;
        }

        var movement = new GenericMovementGenerator(initializer, MovementGeneratorType.Effect, id, arrivalSpellId, arrivalSpellTargetGuid)
        {
            Priority = MovementGeneratorPriority.Highest,
            BaseUnitState = UnitState.Jumping
        };

        movement.AddFlag(MovementGeneratorFlags.PersistOnDeath);
        Add(movement);
    }

    public void MoveKnockbackFrom(Position origin, float speedXy, float speedZ, SpellEffectExtraData spellEffectExtraData = null)
    {
        //This function may make players fall below map
        if (Owner.IsTypeId(TypeId.Player))
            return;

        if (speedXy < 0.01f)
            return;

        Position dest = Owner.Location;
        var moveTimeHalf = (float)(speedZ / GRAVITY);
        var dist = 2 * moveTimeHalf * speedXy;
        var maxHeight = -MoveSpline.ComputeFallElevation(moveTimeHalf, false, -speedZ);

        // Use a mmap raycast to get a valid destination.
        Owner.MovePositionToFirstCollision(dest, dist, Owner.Location.GetRelativeAngle(origin) + MathF.PI);

        var initializer = (MoveSplineInit init) =>
        {
            init.MoveTo(dest.X, dest.Y, dest.Z, false);
            init.SetParabolic(maxHeight, 0);
            init.SetOrientationFixed(true);
            init.SetVelocity(speedXy);

            if (spellEffectExtraData != null)
                init.SetSpellEffectExtraData(spellEffectExtraData);
        };

        GenericMovementGenerator movement = new(initializer, MovementGeneratorType.Effect, 0)
        {
            Priority = MovementGeneratorPriority.Highest
        };

        movement.AddFlag(MovementGeneratorFlags.PersistOnDeath);
        Add(movement);
    }

    public void MoveLand(uint id, Position pos, float? velocity = null)
    {
        var initializer = (MoveSplineInit init) =>
        {
            init.MoveTo(pos, false);
            init.SetAnimation(AnimTier.Ground);

            if (velocity.HasValue)
                init.SetVelocity(velocity.Value);
        };

        Add(new GenericMovementGenerator(initializer, MovementGeneratorType.Effect, id));
    }

    public void MovePath(uint pathId, bool repeatable)
    {
        if (pathId == 0)
            return;

        Add(new WaypointMovementGenerator(pathId, repeatable), MovementSlot.Default);
    }

    public void MovePath(WaypointPath path, bool repeatable)
    {
        Add(new WaypointMovementGenerator(path, repeatable), MovementSlot.Default);
    }

    public void MovePoint(uint id, Position pos, bool generatePath = true, float? finalOrient = null, float speed = 0, MovementWalkRunSpeedSelectionMode speedSelectionMode = MovementWalkRunSpeedSelectionMode.Default, float closeEnoughDistance = 0)
    {
        MovePoint(id, pos.X, pos.Y, pos.Z, generatePath, finalOrient, speed, speedSelectionMode, closeEnoughDistance);
    }

    public void MovePoint(uint id, float x, float y, float z, bool generatePath = true, float? finalOrient = null, float speed = 0, MovementWalkRunSpeedSelectionMode speedSelectionMode = MovementWalkRunSpeedSelectionMode.Default, float closeEnoughDistance = 0)
    {
        Add(new PointMovementGenerator(id, x, y, z, generatePath, speed, finalOrient, null, null, speedSelectionMode, closeEnoughDistance));
    }

    public void MoveRandom(float wanderDistance, TimeSpan duration = default)
    {
        if (Owner.IsTypeId(TypeId.Unit))
            Add(new RandomMovementGenerator(wanderDistance, duration), MovementSlot.Default);
    }

    public void MoveRotate(uint id, uint time, RotateDirection direction)
    {
        if (time == 0)
            return;

        Add(new RotateMovementGenerator(id, time, direction));
    }

    public void MoveSeekAssistance(float x, float y, float z)
    {
        var creature = Owner.AsCreature;

        if (creature != null)
        {
            Log.Logger.Debug($"MotionMaster::MoveSeekAssistance: '{creature.GUID}', seeks assistance (X: {x}, Y: {y}, Z: {z})");
            creature.AttackStop();
            creature.CastStop();
            creature.DoNotReacquireSpellFocusTarget();
            creature.ReactState = ReactStates.Passive;
            Add(new AssistanceMovementGenerator(EventId.AssistMove, x, y, z));
        }
        else
        {
            Log.Logger.Error($"MotionMaster::MoveSeekAssistance: {Owner.GUID}, attempted to seek assistance");
        }
    }

    public void MoveSeekAssistanceDistract(uint time)
    {
        if (Owner.IsCreature)
            Add(new AssistanceDistractMovementGenerator(time, Owner.Location.Orientation));
        else
            Log.Logger.Error($"MotionMaster::MoveSeekAssistanceDistract: {Owner.GUID} attempted to call distract after assistance");
    }

    public void MoveSmoothPath(uint pointId, Vector3[] pathPoints, int pathSize, bool walk = false, bool fly = false)
    {
        var initializer = (MoveSplineInit init) =>
        {
            init.MovebyPath(pathPoints);
            init.SetWalk(walk);

            if (fly)
            {
                init.SetFly();
                init.SetUncompressed();
                init.SetSmooth();
            }
        };

        // This code is not correct
        // GenericMovementGenerator does not affect UNIT_STATE_ROAMING_MOVE
        // need to call PointMovementGenerator with various pointIds
        Add(new GenericMovementGenerator(initializer, MovementGeneratorType.Effect, pointId));
    }

    public void MoveTakeoff(uint id, Position pos, float? velocity = null)
    {
        var initializer = (MoveSplineInit init) =>
        {
            init.MoveTo(pos, false);
            init.SetAnimation(AnimTier.Hover);

            if (velocity.HasValue)
                init.SetVelocity(velocity.Value);
        };

        Add(new GenericMovementGenerator(initializer, MovementGeneratorType.Effect, id));
    }

    public void MoveTargetedHome()
    {
        var owner = Owner.AsCreature;

        if (owner == null)
        {
            Log.Logger.Error($"MotionMaster::MoveTargetedHome: '{Owner.GUID}', attempted to move towards target home.");

            return;
        }

        Clear();

        var target = owner.CharmerOrOwner;

        if (target == null)
            Add(new HomeMovementGenerator<Creature>());
        else
            Add(new FollowMovementGenerator(target, SharedConst.PetFollowDist, new ChaseAngle(SharedConst.PetFollowAngle)));
    }

    public void MoveTaxiFlight(uint path, uint pathnode)
    {
        if (Owner.IsTypeId(TypeId.Player))
        {
            if (path < CliDB.TaxiPathNodesByPath.Count)
            {
                Log.Logger.Debug($"MotionMaster::MoveTaxiFlight: {Owner.GUID} taxi to Path Id: {path} (node {pathnode})");

                // Only one FLIGHT_MOTION_TYPE is allowed
                var hasExisting = HasMovementGenerator(gen => gen.GetMovementGeneratorType() == MovementGeneratorType.Flight);

                FlightPathMovementGenerator movement = new();
                movement.LoadPath(Owner.AsPlayer);
                Add(movement);
            }
            else
            {
                Log.Logger.Error($"MotionMaster::MoveTaxiFlight: '{Owner.GUID}', attempted taxi to non-existing path Id: {path} (node: {pathnode})");
            }
        }
        else
        {
            Log.Logger.Error($"MotionMaster::MoveTaxiFlight: '{Owner.GUID}', attempted taxi to path Id: {path} (node: {pathnode})");
        }
    }

    public void PropagateSpeedChange()
    {
        if (Empty())
            return;

        var movement = GetCurrentMovementGenerator();

        movement?.UnitSpeedChanged();
    }

    public void Remove(MovementGenerator movement, MovementSlot slot = MovementSlot.Active)
    {
        if (movement == null || IsInvalidMovementSlot(slot))
            return;

        if (HasFlag(MotionMasterFlags.Delayed))
        {
            DelayedActions.Enqueue(new DelayedAction(() => Remove(movement, slot), MotionMasterDelayedActionType.Remove));

            return;
        }

        if (Empty())
            return;

        switch (slot)
        {
            case MovementSlot.Default:
                if (DefaultGenerator != null && DefaultGenerator == movement)
                    DirectClearDefault();

                break;

            case MovementSlot.Active:
                lock (Generators)
                {
                    if (!Generators.Empty())
                        if (Generators.Contains(movement))
                            Remove(movement, GetCurrentMovementGenerator() == movement, false);
                }

                break;

            default:
                break;
        }
    }

    public void Remove(MovementGeneratorType type, MovementSlot slot = MovementSlot.Active)
    {
        if (IsInvalidMovementGeneratorType(type) || IsInvalidMovementSlot(slot))
            return;

        if (HasFlag(MotionMasterFlags.Delayed))
        {
            DelayedActions.Enqueue(new DelayedAction(() => Remove(type, slot), MotionMasterDelayedActionType.RemoveType));

            return;
        }

        if (Empty())
            return;

        switch (slot)
        {
            case MovementSlot.Default:
                if (DefaultGenerator != null && DefaultGenerator.GetMovementGeneratorType() == type)
                    DirectClearDefault();

                break;

            case MovementSlot.Active:
                lock (Generators)
                {
                    if (!Generators.Empty())
                    {
                        var itr = Generators.FirstOrDefault(a => a.GetMovementGeneratorType() == type);

                        if (itr != null)
                            Remove(itr, GetCurrentMovementGenerator() == itr, false);
                    }
                }

                break;

            default:
                break;
        }
    }

    public int Size()
    {
        lock (Generators)
        {
            return (DefaultGenerator != null ? 1 : 0) + Generators.Count;
        }
    }

    public bool StopOnDeath()
    {
        var movementGenerator = GetCurrentMovementGenerator();

        if (movementGenerator != null)
            if (movementGenerator.HasFlag(MovementGeneratorFlags.PersistOnDeath))
                return false;

        if (Owner.Location.IsInWorld)
        {
            // Only clear MotionMaster for entities that exists in world
            // Avoids crashes in the following conditions :
            //  * Using 'call pet' on dead pets
            //  * Using 'call stabled pet'
            //  * Logging in with dead pets
            Clear();
            MoveIdle();
        }

        Owner.StopMoving();

        return true;
    }

    public void Update(uint diff)
    {
        try
        {
            if (!Owner)
                return;

            if (HasFlag(MotionMasterFlags.InitializationPending | MotionMasterFlags.Initializing))
                return;

            AddFlag(MotionMasterFlags.Update);

            var top = GetCurrentMovementGenerator();

            if (HasFlag(MotionMasterFlags.StaticInitializationPending) && IsStatic(top))
            {
                RemoveFlag(MotionMasterFlags.StaticInitializationPending);
                top.Initialize(Owner);
            }

            if (top.HasFlag(MovementGeneratorFlags.InitializationPending))
                top.Initialize(Owner);

            if (top.HasFlag(MovementGeneratorFlags.Deactivated))
                top.Reset(Owner);

            if (!top.Update(Owner, diff))
                // Since all the actions that modify any slot are delayed, this movement is guaranteed to be top
                lock (Generators)
                {
                    Pop(true, true); // Natural, and only, call to MovementInform
                }

            ResolveDelayedActions();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex);
        }
        finally
        {
            RemoveFlag(MotionMasterFlags.Update);
        }
    }

    private void Add(MovementGenerator movement, MovementSlot slot = MovementSlot.Active)
    {
        if (movement == null)
            return;

        if (IsInvalidMovementSlot(slot))
            return;

        if (HasFlag(MotionMasterFlags.Delayed))

            DelayedActions.Enqueue(new DelayedAction(() => Add(movement, slot), MotionMasterDelayedActionType.Add));
        else
            DirectAdd(movement, slot);
    }

    private void AddBaseUnitState(MovementGenerator movement)
    {
        if (movement == null || movement.BaseUnitState == 0)
            return;

        lock (BaseUnitStatesMap)
        {
            BaseUnitStatesMap.Add((uint)movement.BaseUnitState, movement);
        }

        Owner.AddUnitState(movement.BaseUnitState);
    }

    private void AddFlag(MotionMasterFlags flag)
    {
        Flags |= flag;
    }

    private void ClearBaseUnitState(MovementGenerator movement)
    {
        if (movement == null || movement.BaseUnitState == 0)
            return;

        lock (BaseUnitStatesMap)
        {
            BaseUnitStatesMap.Remove((uint)movement.BaseUnitState, movement);
        }

        if (!BaseUnitStatesMap.ContainsKey((uint)movement.BaseUnitState))
            Owner.ClearUnitState(movement.BaseUnitState);
    }

    private void ClearBaseUnitStates()
    {
        uint unitState = 0;

        lock (BaseUnitStatesMap)
        {
            foreach (var itr in BaseUnitStatesMap.KeyValueList)
                unitState |= itr.Key;

            Owner.ClearUnitState((UnitState)unitState);
            BaseUnitStatesMap.Clear();
        }
    }

    private void Delete(MovementGenerator movement, bool active, bool movementInform)
    {
        movement.Finalize(Owner, active, movementInform);
        ClearBaseUnitState(movement);
    }

    private void DeleteDefault(bool active, bool movementInform)
    {
        DefaultGenerator.Finalize(Owner, active, movementInform);
        DefaultGenerator = GetIdleMovementGenerator();
        AddFlag(MotionMasterFlags.StaticInitializationPending);
    }

    private void DirectAdd(MovementGenerator movement, MovementSlot slot = MovementSlot.Active)
    {
        /*
        IMovementGenerator curr = _slot[(int)slot];
        if (curr != null)
        {
            _slot[(int)slot] = null; // in case a new one is generated in this slot during directdelete
            if (_top == (int)slot && Convert.ToBoolean(_cleanFlag & MotionMasterCleanFlag.Update))
                DelayedDelete(curr);
            else
                DirectDelete(curr);
        }
        else if (_top < (int)slot)
        {
            _top = (int)slot;
        }

        _slot[(int)slot] = m;
        if (_top > (int)slot)
            _initialize[(int)slot] = true;
        else
        {
            _initialize[(int)slot] = false;
            m.Initialize(_owner);
        }
        */

        /*
      * NOTE: This mimics old behaviour: only one MOTION_SLOT_IDLE, MOTION_SLOT_ACTIVE, MOTION_SLOT_CONTROLLED
      * On future changes support for multiple will be added
      */
        switch (slot)
        {
            case MovementSlot.Default:
                if (DefaultGenerator != null)
                    lock (Generators)
                    {
                        DefaultGenerator.Finalize(Owner, Generators.Empty(), false);
                    }

                DefaultGenerator = movement;

                if (IsStatic(movement))
                    AddFlag(MotionMasterFlags.StaticInitializationPending);

                break;

            case MovementSlot.Active:
                lock (Generators)
                {
                    if (!Generators.Empty())
                    {
                        if (movement.Priority >= Generators.FirstOrDefault().Priority)
                        {
                            var itr = Generators.FirstOrDefault();

                            if (movement.Priority == itr.Priority)
                                Remove(itr, true, false);
                            else
                                itr.Deactivate(Owner);
                        }
                        else
                        {
                            var pointer = Generators.FirstOrDefault(a => a.Priority == movement.Priority);

                            if (pointer != null)
                                Remove(pointer, false, false);
                        }
                    }
                    else
                    {
                        DefaultGenerator.Deactivate(Owner);
                    }

                    Generators.Add(movement);
                }

                AddBaseUnitState(movement);

                break;
        }
    }

    private void DirectClear()
    {
        lock (Generators)
        {
            // First delete Top
            if (!Generators.Empty())
                Pop(true, false);

            // Then the rest
            while (!Generators.Empty())
                Pop(false, false);
        }

        // Make sure the storage is empty
        ClearBaseUnitStates();
    }

    private void DirectClear(Func<MovementGenerator, bool> filter)
    {
        if (Generators.Empty())
            return;

        var top = GetCurrentMovementGenerator();

        foreach (var movement in Generators.ToList())
            if (filter(movement))
            {
                Generators.Remove(movement);
                Delete(movement, movement == top, false);
            }
    }

    private void DirectClearDefault()
    {
        if (DefaultGenerator != null)
            DeleteDefault(Generators.Empty(), false);
    }

    private void DirectInitialize()
    {
        // Clear ALL movement generators (including default)
        DirectClearDefault();
        DirectClear();
        InitializeDefault();
    }

    private bool HasFlag(MotionMasterFlags flag)
    {
        return (Flags & flag) != 0;
    }

    private void MoveAlongSplineChain(uint pointId, List<SplineChainLink> chain, bool walk)
    {
        Add(new SplineChainMovementGenerator(pointId, chain, walk));
    }

    private void Pop(bool active, bool movementInform)
    {
        if (!Generators.Empty())
            Remove(Generators.FirstOrDefault(), active, movementInform);
    }

    private void Remove(MovementGenerator movement, bool active, bool movementInform)
    {
        Generators.Remove(movement);
        Delete(movement, active, movementInform);
    }

    private void RemoveFlag(MotionMasterFlags flag)
    {
        Flags &= ~flag;
    }

    private void ResolveDelayedActions()
    {
        while (DelayedActions.Count != 0)
            if (DelayedActions.TryDequeue(out var action) && action != null)
                action.Resolve();
    }

    private void ResumeSplineChain(SplineChainResumeInfo info)
    {
        if (info.Empty())
        {
            Log.Logger.Error("MotionMaster.ResumeSplineChain: unit with entry {0} tried to resume a spline chain from empty info.", Owner.Entry);

            return;
        }

        Add(new SplineChainMovementGenerator(info));
    }
}