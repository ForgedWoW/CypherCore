// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Events;
using Forged.MapServer.Maps;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Entities.GameObjects;

internal class TransportGameObject : GameObjectTypeBase, ITransport
{
    private static readonly TimeSpan PositionUpdateInterval = TimeSpan.FromMilliseconds(50);
    private readonly TransportAnimation _animationInfo;
    private readonly List<WorldObject> _passengers = new();
    private readonly TimeTracker _positionUpdateTimer = new();
    private readonly List<uint> _stopFrames = new();
    private bool _autoCycleBetweenStopFrames;
    private uint _pathProgress;
    private uint _stateChangeProgress;
    private uint _stateChangeTime;

    public TransportGameObject(GameObject owner) : base(owner)
    {
        _animationInfo = Global.TransportMgr.GetTransportAnimInfo(owner.Template.entry);
        _pathProgress = GameTime.CurrentTimeMS % GetTransportPeriod();
        _stateChangeTime = GameTime.CurrentTimeMS;
        _stateChangeProgress = _pathProgress;

        var goInfo = Owner.Template;

        if (goInfo.Transport.Timeto2ndfloor > 0)
        {
            _stopFrames.Add(goInfo.Transport.Timeto2ndfloor);

            if (goInfo.Transport.Timeto3rdfloor > 0)
            {
                _stopFrames.Add(goInfo.Transport.Timeto3rdfloor);

                if (goInfo.Transport.Timeto4thfloor > 0)
                {
                    _stopFrames.Add(goInfo.Transport.Timeto4thfloor);

                    if (goInfo.Transport.Timeto5thfloor > 0)
                    {
                        _stopFrames.Add(goInfo.Transport.Timeto5thfloor);

                        if (goInfo.Transport.Timeto6thfloor > 0)
                        {
                            _stopFrames.Add(goInfo.Transport.Timeto6thfloor);

                            if (goInfo.Transport.Timeto7thfloor > 0)
                            {
                                _stopFrames.Add(goInfo.Transport.Timeto7thfloor);

                                if (goInfo.Transport.Timeto8thfloor > 0)
                                {
                                    _stopFrames.Add(goInfo.Transport.Timeto8thfloor);

                                    if (goInfo.Transport.Timeto9thfloor > 0)
                                    {
                                        _stopFrames.Add(goInfo.Transport.Timeto9thfloor);

                                        if (goInfo.Transport.Timeto10thfloor > 0)
                                            _stopFrames.Add(goInfo.Transport.Timeto10thfloor);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (!_stopFrames.Empty())
        {
            _pathProgress = 0;
            _stateChangeProgress = 0;
        }

        _positionUpdateTimer.Reset(PositionUpdateInterval);
    }

    public void AddPassenger(WorldObject passenger)
    {
        if (!Owner.Location.IsInWorld)
            return;

        if (!_passengers.Contains(passenger))
        {
            _passengers.Add(passenger);
            passenger.Transport = this;
            passenger.MovementInfo.Transport.Guid = GetTransportGUID();
            Log.Logger.Debug($"Object {passenger.GetName()} boarded transport {Owner.GetName()}.");
        }
    }

    public void CalculatePassengerOffset(Position pos)
    {
        ITransport.CalculatePassengerOffset(pos, Owner.Location.X, Owner.Location.Y, Owner.Location.Z, Owner.Location.Orientation);
    }

    public void CalculatePassengerPosition(Position pos)
    {
        ITransport.CalculatePassengerPosition(pos, Owner.Location.X, Owner.Location.Y, Owner.Location.Z, Owner.Location.Orientation);
    }

    public int GetMapIdForSpawning()
    {
        return Owner.Template.Transport.SpawnMap;
    }

    public List<uint> GetPauseTimes()
    {
        return _stopFrames;
    }

    public ObjectGuid GetTransportGUID()
    {
        return Owner.GUID;
    }

    public float GetTransportOrientation()
    {
        return Owner.Location.Orientation;
    }
    public uint GetTransportPeriod()
    {
        if (_animationInfo != null)
            return _animationInfo.TotalTime;

        return 1;
    }

    public override void OnRelocated()
    {
        UpdatePassengerPositions();
    }

    public override void OnStateChanged(GameObjectState oldState, GameObjectState newState)
    {
        if (_stopFrames.Empty())
        {
            if (newState != GameObjectState.TransportActive)
                Owner.SetGoState(GameObjectState.TransportActive);

            return;
        }

        uint stopPathProgress = 0;

        if (newState != GameObjectState.TransportActive)
        {
            var stopFrame = newState - GameObjectState.TransportStopped;
            stopPathProgress = _stopFrames[stopFrame];
        }

        _stateChangeTime = GameTime.CurrentTimeMS;
        _stateChangeProgress = _pathProgress;
        var timeToStop = (uint)Math.Abs(_pathProgress - stopPathProgress);
        Owner.SetLevel(GameTime.CurrentTimeMS + timeToStop);
        Owner.SetPathProgressForClient(_pathProgress / (float)GetTransportPeriod());

        if (oldState == GameObjectState.Active || oldState == newState)
        {
            // initialization
            if (_pathProgress > stopPathProgress)
                Owner.SetDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);
            else
                Owner.RemoveDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);

            return;
        }

        var pauseTimesCount = _stopFrames.Count;
        var newToOldStateDelta = newState - oldState;

        if (newToOldStateDelta < 0)
            newToOldStateDelta += pauseTimesCount + 1;

        var oldToNewStateDelta = oldState - newState;

        if (oldToNewStateDelta < 0)
            oldToNewStateDelta += pauseTimesCount + 1;

        // this additional check is neccessary because client doesn't check dynamic flags on progress update
        // instead it multiplies progress from dynamicflags field by -1 and then compares that against 0
        // when calculating path progress while we simply check the Id if (!_owner.HasDynamicFlag(GO_DYNFLAG_LO_INVERTED_MOVEMENT))
        var isAtStartOfPath = _stateChangeProgress == 0;

        if (oldToNewStateDelta < newToOldStateDelta && !isAtStartOfPath)
            Owner.SetDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);
        else
            Owner.RemoveDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement);
    }

    public ITransport RemovePassenger(WorldObject passenger)
    {
        if (_passengers.Remove(passenger))
        {
            passenger.Transport = null;
            passenger.MovementInfo.Transport.Reset();
            Log.Logger.Debug($"Object {passenger.GetName()} removed from transport {Owner.GetName()}.");

            var plr = passenger.AsPlayer;

            plr?.SetFallInformation(0, plr.Location.Z);
        }

        return this;
    }
    public void SetAutoCycleBetweenStopFrames(bool on)
    {
        _autoCycleBetweenStopFrames = on;
    }

    public override void Update(uint diff)
    {
        if (_animationInfo == null)
            return;

        _positionUpdateTimer.Update(diff);

        if (!_positionUpdateTimer.Passed)
            return;

        _positionUpdateTimer.Reset(PositionUpdateInterval);

        var now = GameTime.CurrentTimeMS;
        var period = GetTransportPeriod();
        uint newProgress = 0;

        if (_stopFrames.Empty())
        {
            newProgress = now % period;
        }
        else
        {
            var stopTargetTime = 0;

            if (Owner.GoState == GameObjectState.TransportActive)
                stopTargetTime = 0;
            else
                stopTargetTime = (int)(_stopFrames[Owner.GoState - GameObjectState.TransportStopped]);

            if (now < Owner.GameObjectFieldData.Level)
            {
                var timeToStop = (int)(Owner.GameObjectFieldData.Level - _stateChangeTime);
                var stopSourcePathPct = _stateChangeProgress / (float)period;
                var stopTargetPathPct = stopTargetTime / (float)period;
                var timeSinceStopProgressPct = (now - _stateChangeTime) / (float)timeToStop;

                float progressPct;

                if (!Owner.HasDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement))
                {
                    if (Owner.GoState == GameObjectState.TransportActive)
                        stopTargetPathPct = 1.0f;

                    var pathPctBetweenStops = stopTargetPathPct - stopSourcePathPct;

                    if (pathPctBetweenStops < 0.0f)
                        pathPctBetweenStops += 1.0f;

                    progressPct = pathPctBetweenStops * timeSinceStopProgressPct + stopSourcePathPct;

                    if (progressPct > 1.0f)
                        progressPct = progressPct - 1.0f;
                }
                else
                {
                    var pathPctBetweenStops = stopSourcePathPct - stopTargetPathPct;

                    if (pathPctBetweenStops < 0.0f)
                        pathPctBetweenStops += 1.0f;

                    progressPct = stopSourcePathPct - pathPctBetweenStops * timeSinceStopProgressPct;

                    if (progressPct < 0.0f)
                        progressPct += 1.0f;
                }

                newProgress = (uint)(period * progressPct) % period;
            }
            else
            {
                newProgress = (uint)stopTargetTime;
            }

            if (newProgress == stopTargetTime && newProgress != _pathProgress)
            {
                var eventId = (Owner.GoState - GameObjectState.TransportActive) switch
                {
                    0 => Owner.Template.Transport.Reached1stfloor,
                    1 => Owner.Template.Transport.Reached2ndfloor,
                    2 => Owner.Template.Transport.Reached3rdfloor,
                    3 => Owner.Template.Transport.Reached4thfloor,
                    4 => Owner.Template.Transport.Reached5thfloor,
                    5 => Owner.Template.Transport.Reached6thfloor,
                    6 => Owner.Template.Transport.Reached7thfloor,
                    7 => Owner.Template.Transport.Reached8thfloor,
                    8 => Owner.Template.Transport.Reached9thfloor,
                    9 => Owner.Template.Transport.Reached10thfloor,
                    _ => 0u
                };

                if (eventId != 0)
                    GameEvents.Trigger(eventId, Owner, null);

                if (_autoCycleBetweenStopFrames)
                {
                    var currentState = Owner.GoState;
                    GameObjectState newState;

                    if (currentState == GameObjectState.TransportActive)
                        newState = GameObjectState.TransportStopped;
                    else if (currentState - GameObjectState.TransportActive == _stopFrames.Count)
                        newState = currentState - 1;
                    else if (Owner.HasDynamicFlag(GameObjectDynamicLowFlags.InvertedMovement))
                        newState = currentState - 1;
                    else
                        newState = currentState + 1;

                    Owner.SetGoState(newState);
                }
            }
        }

        if (_pathProgress == newProgress)
            return;

        _pathProgress = newProgress;

        var oldAnimation = _animationInfo.GetPrevAnimNode(newProgress);
        var newAnimation = _animationInfo.GetNextAnimNode(newProgress);

        if (oldAnimation != null && newAnimation != null)
        {
            var pathRotation = new Quaternion(Owner.GameObjectFieldData.ParentRotation.Value.X,
                                              Owner.GameObjectFieldData.ParentRotation.Value.Y,
                                              Owner.GameObjectFieldData.ParentRotation.Value.Z,
                                              Owner.GameObjectFieldData.ParentRotation.Value.W).ToMatrix();

            Vector3 prev = new(oldAnimation.Pos.X, oldAnimation.Pos.Y, oldAnimation.Pos.Z);
            Vector3 next = new(newAnimation.Pos.X, newAnimation.Pos.Y, newAnimation.Pos.Z);

            var dst = next;

            if (prev != next)
            {
                var animProgress = (newProgress - oldAnimation.TimeIndex) / (float)(newAnimation.TimeIndex - oldAnimation.TimeIndex);

                dst = pathRotation.Multiply(Vector3.Lerp(prev, next, animProgress));
            }

            dst = pathRotation.Multiply(dst);
            dst += Owner.StationaryPosition;

            Owner.Location.Map.GameObjectRelocation(Owner, dst.X, dst.Y, dst.Z, Owner.Location.Orientation);
        }

        var oldRotation = _animationInfo.GetPrevAnimRotation(newProgress);
        var newRotation = _animationInfo.GetNextAnimRotation(newProgress);

        if (oldRotation != null && newRotation != null)
        {
            Quaternion prev = new(oldRotation.Rot[0], oldRotation.Rot[1], oldRotation.Rot[2], oldRotation.Rot[3]);
            Quaternion next = new(newRotation.Rot[0], newRotation.Rot[1], newRotation.Rot[2], newRotation.Rot[3]);

            var rotation = next;

            if (prev != next)
            {
                var animProgress = (newProgress - oldRotation.TimeIndex) / (float)(newRotation.TimeIndex - oldRotation.TimeIndex);

                rotation = Quaternion.Lerp(prev, next, animProgress);
            }

            Owner.SetLocalRotation(rotation.X, rotation.Y, rotation.Z, rotation.W);
            Owner.UpdateModelPosition();
        }

        // update progress marker for client
        Owner.SetPathProgressForClient(_pathProgress / (float)period);
    }
    public void UpdatePassengerPositions()
    {
        foreach (var passenger in _passengers)
        {
            var pos = passenger.MovementInfo.Transport.Pos.Copy();
            CalculatePassengerPosition(pos);
            ITransport.UpdatePassengerPosition(this, Owner.Location.Map, passenger, pos, true);
        }
    }
}