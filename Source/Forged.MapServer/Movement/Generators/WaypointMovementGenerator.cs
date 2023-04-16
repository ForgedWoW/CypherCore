// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Movement.Generators;

public class WaypointMovementGenerator : MovementGeneratorMedium<Creature>
{
    private readonly bool _loadedFromDB;
    private readonly TimeTracker _nextMoveTime;
    private readonly bool _repeating;
    private int _currentNode;
    private WaypointPath _path;
    private uint _pathId;

    public WaypointMovementGenerator(uint pathId = 0, bool repeating = true)
    {
        _nextMoveTime = new TimeTracker();
        _pathId = pathId;
        _repeating = repeating;
        _loadedFromDB = true;

        Mode = MovementGeneratorMode.Default;
        Priority = MovementGeneratorPriority.Normal;
        Flags = MovementGeneratorFlags.InitializationPending;
        BaseUnitState = UnitState.Roaming;
    }

    public WaypointMovementGenerator(WaypointPath path, bool repeating = true)
    {
        _nextMoveTime = new TimeTracker();
        _repeating = repeating;
        _path = path;

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

        if (!active)
            return;

        owner.ClearUnitState(UnitState.RoamingMove);

        // TODO: Research if this modification is needed, which most likely isnt
        owner.SetWalk(false);
    }

    public override void DoInitialize(Creature owner)
    {
        RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);

        if (_loadedFromDB)
        {
            if (_pathId == 0)
                _pathId = owner.WaypointPath;

            _path = owner.WaypointManager.GetPath(_pathId);
        }

        if (_path == null)
        {
            Log.Logger.Error($"WaypointMovementGenerator::DoInitialize: couldn't load path for creature ({owner.GUID}) (_pathId: {_pathId})");

            return;
        }

        owner.StopMoving();

        _nextMoveTime.Reset(1000);
    }

    public override void DoReset(Creature owner)
    {
        RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);

        owner.StopMoving();

        if (!HasFlag(MovementGeneratorFlags.Finalized) && _nextMoveTime.Passed)
            _nextMoveTime.Reset(1); // Needed so that Update does not behave as if node was reached
    }

    public override bool DoUpdate(Creature owner, uint diff)
    {
        if (owner is not { IsAlive: true })
            return true;

        if (HasFlag(MovementGeneratorFlags.Finalized | MovementGeneratorFlags.Paused) || _path == null || _path.Nodes.Empty())
            return true;

        if (owner.HasUnitState(UnitState.NotMove | UnitState.LostControl) || owner.IsMovementPreventedByCasting())
        {
            AddFlag(MovementGeneratorFlags.Interrupted);
            owner.StopMoving();

            return true;
        }

        if (HasFlag(MovementGeneratorFlags.Interrupted))
        {
            /*
            *  relaunch only if
            *  - has a tiner? -> was it interrupted while not waiting aka moving? need to check both:
            *      -> has a timer - is it because its waiting to start next node?
            *      -> has a timer - is it because something set it while moving (like timed pause)?
            *
            *  - doesnt have a timer? -> is movement valid?
            *
            *  TODO: ((_nextMoveTime.Passed() && VALID_MOVEMENT) || (!_nextMoveTime.Passed() && !HasFlag(MOVEMENTGENERATOR_FLAG_INFORM_ENABLED)))
            */
            if (HasFlag(MovementGeneratorFlags.Initialized) && (_nextMoveTime.Passed || !HasFlag(MovementGeneratorFlags.InformEnabled)))
            {
                StartMove(owner, true);

                return true;
            }

            RemoveFlag(MovementGeneratorFlags.Interrupted);
        }

        // if it's moving
        if (!owner.MoveSpline.Splineflags.HasFlag(SplineFlag.Done))
        {
            // set home position at place (every MotionMaster::UpdateMotion)
            if (owner.GetTransGUID().IsEmpty)
                owner.HomePosition = owner.Location;

            // relaunch movement if its speed has changed
            if (HasFlag(MovementGeneratorFlags.SpeedUpdatePending))
                StartMove(owner, true);
        }
        else if (!_nextMoveTime.Passed) // it's not moving, is there a timer?
        {
            if (UpdateTimer(diff))
            {
                if (!HasFlag(MovementGeneratorFlags.Initialized)) // initial movement call
                {
                    StartMove(owner);

                    return true;
                }

                if (!HasFlag(MovementGeneratorFlags.InformEnabled)) // timer set before node was reached, resume now
                {
                    StartMove(owner, true);

                    return true;
                }
            }
            else
            {
                return true; // keep waiting
            }
        }
        else // not moving, no timer
        {
            if (HasFlag(MovementGeneratorFlags.Initialized) && !HasFlag(MovementGeneratorFlags.InformEnabled))
            {
                OnArrived(owner);                              // hooks and wait timer reset (if necessary)
                AddFlag(MovementGeneratorFlags.InformEnabled); // signals to future StartMove that it reached a node
            }

            if (_nextMoveTime.Passed) // OnArrived might have set a timer
                StartMove(owner);     // check path status, get next point and move if necessary & can
        }

        return true;
    }

    public override string GetDebugInfo()
    {
        return $"Current Node: {_currentNode}\n{base.GetDebugInfo()}";
    }

    public override MovementGeneratorType GetMovementGeneratorType()
    {
        return MovementGeneratorType.Waypoint;
    }

    public override bool GetResetPosition(Unit owner, out float x, out float y, out float z)
    {
        x = y = z = 0;

        // prevent a crash at empty waypoint path.
        if (_path == null || _path.Nodes.Empty())
            return false;

        var waypoint = _path.Nodes.ElementAt(_currentNode);

        x = waypoint.X;
        y = waypoint.Y;
        z = waypoint.Z;

        return true;
    }

    public override void Pause(uint timer = 0)
    {
        if (timer != 0)
        {
            // Don't try to paused an already paused generator
            if (HasFlag(MovementGeneratorFlags.Paused))
                return;

            AddFlag(MovementGeneratorFlags.TimedPaused);
            _nextMoveTime.Reset(timer);
            RemoveFlag(MovementGeneratorFlags.Paused);
        }
        else
        {
            AddFlag(MovementGeneratorFlags.Paused);
            _nextMoveTime.Reset(1); // Needed so that Update does not behave as if node was reached
            RemoveFlag(MovementGeneratorFlags.TimedPaused);
        }
    }

    public override void Resume(uint overrideTimer = 0)
    {
        if (overrideTimer != 0)
            _nextMoveTime.Reset(overrideTimer);

        if (_nextMoveTime.Passed)
            _nextMoveTime.Reset(1); // Needed so that Update does not behave as if node was reached

        RemoveFlag(MovementGeneratorFlags.Paused);
    }

    public override void UnitSpeedChanged()
    {
        AddFlag(MovementGeneratorFlags.SpeedUpdatePending);
    }

    private bool ComputeNextNode()
    {
        if (_currentNode == _path.Nodes.Count - 1 && !_repeating)
            return false;

        _currentNode = (_currentNode + 1) % _path.Nodes.Count;

        return true;
    }

    private void OnArrived(Creature owner)
    {
        if (_path == null || _path.Nodes.Empty())
            return;

        var waypoint = _path.Nodes.ElementAt(_currentNode);

        if (waypoint.Delay != 0)
        {
            owner.ClearUnitState(UnitState.RoamingMove);
            _nextMoveTime.Reset(waypoint.Delay);
        }

        if (waypoint.EventId != 0 && RandomHelper.URand(0, 99) < waypoint.EventChance)
        {
            Log.Logger.Debug($"Creature movement start script {waypoint.EventId} at point {_currentNode} for {owner.GUID}.");
            owner.ClearUnitState(UnitState.RoamingMove);
            owner.Location.Map.ScriptsStart(ScriptsType.Waypoint, waypoint.EventId, owner, null);
        }

        // inform AI
        var ai = owner.AI;

        if (ai != null)
        {
            ai.MovementInform(MovementGeneratorType.Waypoint, (uint)_currentNode);
            ai.WaypointReached(waypoint.ID, _path.ID);
        }

        owner.UpdateCurrentWaypointInfo(waypoint.ID, _path.ID);
    }

    private void StartMove(Creature owner, bool relaunch = false)
    {
        // sanity checks
        if (owner is not { IsAlive: true } || HasFlag(MovementGeneratorFlags.Finalized) || _path == null || _path.Nodes.Empty() || (relaunch && (HasFlag(MovementGeneratorFlags.InformEnabled) || !HasFlag(MovementGeneratorFlags.Initialized))))
            return;

        if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting() || (owner.IsFormationLeader && !owner.IsFormationLeaderMoveAllowed)) // if cannot move OR cannot move because of formation
        {
            _nextMoveTime.Reset(1000); // delay 1s

            return;
        }

        var transportPath = !owner.GetTransGUID().IsEmpty;

        if (HasFlag(MovementGeneratorFlags.InformEnabled) && HasFlag(MovementGeneratorFlags.Initialized))
        {
            if (ComputeNextNode())
            {
                // inform AI
                var ai = owner.AI;

                ai?.WaypointStarted(_path.Nodes[_currentNode].ID, _path.ID);
            }
            else
            {
                var currentWaypoint = _path.Nodes[_currentNode];
                var pos = new Position(currentWaypoint.X, currentWaypoint.Y, currentWaypoint.Z, owner.Location.Orientation);

                if (!transportPath)
                {
                    owner.HomePosition = pos;
                }
                else
                {
                    var trans = owner.Transport;

                    if (trans != null)
                    {
                        pos.Orientation -= trans.GetTransportOrientation();
                        owner.TransportHomePosition = pos;
                        trans.CalculatePassengerPosition(pos);
                        owner.HomePosition = pos;
                    }
                    // else if (vehicle) - this should never happen, vehicle offsets are const
                }

                AddFlag(MovementGeneratorFlags.Finalized);
                owner.UpdateCurrentWaypointInfo(0, 0);

                // inform AI
                var ai = owner.AI;

                ai?.WaypointPathEnded(currentWaypoint.ID, _path.ID);

                return;
            }
        }
        else if (!HasFlag(MovementGeneratorFlags.Initialized))
        {
            AddFlag(MovementGeneratorFlags.Initialized);

            // inform AI
            var ai = owner.AI;

            ai?.WaypointStarted(_path.Nodes[_currentNode].ID, _path.ID);
        }

        var waypoint = _path.Nodes[_currentNode];

        RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.InformEnabled | MovementGeneratorFlags.TimedPaused);

        owner.AddUnitState(UnitState.RoamingMove);

        MoveSplineInit init = new(owner);

        //! If creature is on transport, we assume waypoints set in DB are already transport offsets
        if (transportPath)
            init.DisableTransportPathTransformations();

        //! Do not use formationDest here, MoveTo requires transport offsets due to DisableTransportPathTransformations() call
        //! but formationDest contains global coordinates
        init.MoveTo(waypoint.X, waypoint.Y, waypoint.Z);

        if (waypoint.Orientation.HasValue && waypoint.Delay != 0)
            init.SetFacing(waypoint.Orientation.Value);

        switch (waypoint.MoveType)
        {
            case WaypointMoveType.Land:
                init.SetAnimation(AnimTier.Ground);

                break;

            case WaypointMoveType.Takeoff:
                init.SetAnimation(AnimTier.Hover);

                break;

            case WaypointMoveType.Run:
                init.SetWalk(false);

                break;

            case WaypointMoveType.Walk:
                init.SetWalk(true);

                break;
        }

        init.Launch();

        // inform formation
        owner.SignalFormationMovement();
    }

    private bool UpdateTimer(uint diff)
    {
        _nextMoveTime.Update(diff);

        if (!_nextMoveTime.Passed)
            return false;

        _nextMoveTime.Reset(0);

        return true;

    }
}