// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.DataStorage.Structs.T;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Movement.Generators;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Instance;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Movement;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Framework.Collections;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class MovementHandler : IWorldSessionHandler
{
    private readonly CliDB _cliDB;
    private readonly DB2Manager _db2Manager;
    private readonly GridDefines _gridDefines;
    private readonly InstanceLockManager _instanceLockManager;
    private readonly MapManager _mapManager;
    private readonly DB6Storage<MapRecord> _mapRecords;
    private readonly Dictionary<uint, uint> _pendingTimeSyncRequests = new();
    private readonly WorldSession _session;
    private readonly DB6Storage<TaxiNodesRecord> _taxiNodesRecords;
    private readonly CircularBuffer<Tuple<long, uint>> _timeSyncClockDeltaQueue = new(6);
    private readonly UnitCombatHelpers _unitCombatHelpers;
    private long _timeSyncClockDelta;
    // key: counter. value: server time when packet with that counter was sent.

    public MovementHandler(WorldSession session, GridDefines gridDefines, UnitCombatHelpers unitCombatHelpers, DB6Storage<TaxiNodesRecord> taxiNodesRecords, DB6Storage<MapRecord> mapRecords,
                           MapManager mapManager, CliDB cliDB, DB2Manager db2Manager, InstanceLockManager instanceLockManager)
    {
        _session = session;
        _gridDefines = gridDefines;
        _unitCombatHelpers = unitCombatHelpers;
        _taxiNodesRecords = taxiNodesRecords;
        _mapRecords = mapRecords;
        _mapManager = mapManager;
        _cliDB = cliDB;
        _db2Manager = db2Manager;
        _instanceLockManager = instanceLockManager;
    }

    private uint AdjustClientMovementTime(uint time)
    {
        var movementTime = time + _timeSyncClockDelta;

        if (_timeSyncClockDelta == 0 || movementTime is < 0 or > 0xFFFFFFFF)
        {
            Log.Logger.Warning("The computed movement time using clockDelta is erronous. Using fallback instead");

            return GameTime.CurrentTimeMS;
        }
        else
        {
            return (uint)movementTime;
        }
    }

    private void ComputeNewClockDelta()
    {
        // implementation of the technique described here: https://web.archive.org/web/20180430214420/http://www.mine-control.com/zack/timesync/timesync.html
        // to reduce the skew induced by dropped TCP packets that get resent.

        //accumulator_set < uint32, features < tag::mean, tag::median, tag::variance(lazy) > > latencyAccumulator;
        List<uint> latencyList = new();

        foreach (var pair in _timeSyncClockDeltaQueue)
            latencyList.Add(pair.Item2);

        var latencyMedian = (uint)Math.Round(latencyList.Average(p => p));                  //median(latencyAccumulator));
        var latencyStandardDeviation = (uint)Math.Round(Math.Sqrt(latencyList.Variance())); //variance(latencyAccumulator)));

        //accumulator_set<long, features<tag::mean>> clockDeltasAfterFiltering;
        List<long> clockDeltasAfterFiltering = new();
        uint sampleSizeAfterFiltering = 0;

        foreach (var pair in _timeSyncClockDeltaQueue)
            if (pair.Item2 < latencyStandardDeviation + latencyMedian)
            {
                clockDeltasAfterFiltering.Add(pair.Item1);
                sampleSizeAfterFiltering++;
            }

        if (sampleSizeAfterFiltering != 0)
        {
            var meanClockDelta = (long)Math.Round(clockDeltasAfterFiltering.Average());

            if (Math.Abs(meanClockDelta - _timeSyncClockDelta) > 25)
                _timeSyncClockDelta = meanClockDelta;
        }
        else if (_timeSyncClockDelta == 0)
        {
            var back = _timeSyncClockDeltaQueue.Back();
            _timeSyncClockDelta = back.Item1;
        }
    }

    [WorldPacketHandler(ClientOpcodes.MoveForceFlightBackSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveForceFlightSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveForcePitchRateChangeAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveForceRunBackSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveForceRunSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveForceSwimBackSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveForceSwimSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveForceTurnRateChangeAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveForceWalkSpeedChangeAck, Processing = PacketProcessing.ThreadSafe)]
    private void HandleForceSpeedChangeAck(MovementSpeedAck packet)
    {
        _session.Player.ValidateMovementInfo(packet.Ack.Status);

        // now can skip not our packet
        if (_session.Player.GUID != packet.Ack.Status.Guid)
            return;

        /*----------------*/
        // client ACK send one packet for mounted/run case and need skip all except last from its
        // in other cases anti-cheat check can be fail in false case
        UnitMoveType unitMoveType;

        var opcode = packet.GetOpcode();

        switch (opcode)
        {
            case ClientOpcodes.MoveForceWalkSpeedChangeAck:
                unitMoveType = UnitMoveType.Walk;

                break;

            case ClientOpcodes.MoveForceRunSpeedChangeAck:
                unitMoveType = UnitMoveType.Run;

                break;

            case ClientOpcodes.MoveForceRunBackSpeedChangeAck:
                unitMoveType = UnitMoveType.RunBack;

                break;

            case ClientOpcodes.MoveForceSwimSpeedChangeAck:
                unitMoveType = UnitMoveType.Swim;

                break;

            case ClientOpcodes.MoveForceSwimBackSpeedChangeAck:
                unitMoveType = UnitMoveType.SwimBack;

                break;

            case ClientOpcodes.MoveForceTurnRateChangeAck:
                unitMoveType = UnitMoveType.TurnRate;

                break;

            case ClientOpcodes.MoveForceFlightSpeedChangeAck:
                unitMoveType = UnitMoveType.Flight;

                break;

            case ClientOpcodes.MoveForceFlightBackSpeedChangeAck:
                unitMoveType = UnitMoveType.FlightBack;

                break;

            case ClientOpcodes.MoveForcePitchRateChangeAck:
                unitMoveType = UnitMoveType.PitchRate;

                break;

            default:
                Log.Logger.Error("WorldSession.HandleForceSpeedChangeAck: Unknown move type opcode: {0}", opcode);

                return;
        }

        // skip all forced speed changes except last and unexpected
        // in run/mounted case used one ACK and it must be skipped. m_forced_speed_changes[MOVE_RUN] store both.
        if (_session.Player.ForcedSpeedChanges[(int)unitMoveType] > 0)
        {
            --_session.Player.ForcedSpeedChanges[(int)unitMoveType];

            if (_session.Player.ForcedSpeedChanges[(int)unitMoveType] > 0)
                return;
        }

        if (_session.Player.Transport != null || !(Math.Abs(_session.Player.GetSpeed(unitMoveType) - packet.Speed) > 0.01f))
            return;

        if (_session.Player.GetSpeed(unitMoveType) > packet.Speed) // must be greater - just correct
        {
            Log.Logger.Error("{0} SpeedChange player {1} is NOT correct (must be {2} instead {3}), force set to correct value",
                             unitMoveType,
                             _session.Player.GetName(),
                             _session.Player.GetSpeed(unitMoveType),
                             packet.Speed);

            _session.Player.SetSpeedRate(unitMoveType, _session.Player.GetSpeedRate(unitMoveType));
        }
        else // must be lesser - cheating
        {
            Log.Logger.Debug("Player {0} from account id {1} kicked for incorrect speed (must be {2} instead {3})",
                             _session.Player.GetName(),
                             _session.AccountId,
                             _session.Player.GetSpeed(unitMoveType),
                             packet.Speed);

            _session.KickPlayer("WorldSession::HandleForceSpeedChangeAck Incorrect speed");
        }
    }

    [WorldPacketHandler(ClientOpcodes.MoveApplyMovementForceAck, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveApplyMovementForceAck(MoveApplyMovementForceAck moveApplyMovementForceAck)
    {
        var mover = _session.Player.UnitBeingMoved;
        _session.Player.ValidateMovementInfo(moveApplyMovementForceAck.Ack.Status);

        // prevent tampered movement data
        if (moveApplyMovementForceAck.Ack.Status.Guid != mover.GUID)
        {
            Log.Logger.Error($"HandleMoveApplyMovementForceAck: guid error, expected {mover.GUID}, got {moveApplyMovementForceAck.Ack.Status.Guid}");

            return;
        }

        moveApplyMovementForceAck.Ack.Status.Time = AdjustClientMovementTime(moveApplyMovementForceAck.Ack.Status.Time);

        mover.SendMessageToSet(new MoveUpdateApplyMovementForce()
        {
            Status = moveApplyMovementForceAck.Ack.Status,
            Force = moveApplyMovementForceAck.Force
        }, false);
    }

    [WorldPacketHandler(ClientOpcodes.MoveInitActiveMoverComplete, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveInitActiveMoverComplete(MoveInitActiveMoverComplete moveInitActiveMoverComplete)
    {
        _session.Player.SetPlayerLocalFlag(PlayerLocalFlags.OverrideTransportServerTime);
        _session.Player.SetTransportServerTime((int)(GameTime.CurrentTimeMS - moveInitActiveMoverComplete.Ticks));

        _session.Player.UpdateObjectVisibility(false);
    }

    [WorldPacketHandler(ClientOpcodes.MoveKnockBackAck, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveKnockBackAck(MoveKnockBackAck movementAck)
    {
        _session.Player.ValidateMovementInfo(movementAck.Ack.Status);

        if (_session.Player.UnitBeingMoved.GUID != movementAck.Ack.Status.Guid)
            return;

        movementAck.Ack.Status.Time = AdjustClientMovementTime(movementAck.Ack.Status.Time);
        _session.Player.MovementInfo = movementAck.Ack.Status;

        MoveUpdateKnockBack updateKnockBack = new()
        {
            Status = _session.Player.MovementInfo
        };

        _session.Player.SendMessageToSet(updateKnockBack, false);
    }

    [WorldPacketHandler(ClientOpcodes.MoveChangeTransport, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveDoubleJump, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveFallLand, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveFallReset, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveHeartbeat, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveJump, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveSetFacing, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveSetFacingHeartbeat, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveSetFly, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveSetPitch, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveSetRunMode, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveSetWalkMode, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStartAscend, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStartBackward, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStartDescend, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStartForward, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStartPitchDown, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStartPitchUp, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStartStrafeLeft, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStartStrafeRight, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStartSwim, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStartTurnLeft, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStartTurnRight, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStop, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStopAscend, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStopPitch, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStopStrafe, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStopSwim, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveStopTurn, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveUpdateFallSpeed, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMovement(ClientPlayerMovement packet)
    {
        HandleMovementOpcode(packet.GetOpcode(), packet.Status);
    }

    [WorldPacketHandler(ClientOpcodes.MoveEnableDoubleJumpAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveEnableSwimToFlyTransAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveFeatherFallAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveForceRootAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveForceUnrootAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveGravityDisableAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveGravityEnableAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveHoverAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveSetCanFlyAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveSetCanTurnWhileFallingAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveSetIgnoreMovementForcesAck, Processing = PacketProcessing.ThreadSafe)]
    [WorldPacketHandler(ClientOpcodes.MoveWaterWalkAck, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMovementAckMessage(MovementAckMessage movementAck)
    {
        _session.Player.ValidateMovementInfo(movementAck.Ack.Status);
    }

    public void HandleMovementOpcode(ClientOpcodes opcode, MovementInfo movementInfo)
    {
        var mover = _session.Player.UnitBeingMoved;
        var plrMover = mover.AsPlayer;

        if (plrMover is { IsBeingTeleported: true })
            return;

        _session.Player.ValidateMovementInfo(movementInfo);

        if (movementInfo.Guid != mover.GUID)
        {
            Log.Logger.Error("HandleMovementOpcodes: guid error");

            return;
        }

        if (!movementInfo.Pos.IsPositionValid)
            return;

        if (!mover.MoveSpline.Splineflags.HasFlag(SplineFlag.Done))
            return;

        // stop some emotes at player move
        if (plrMover != null && plrMover.EmoteState != 0)
            plrMover.EmoteState = Emote.OneshotNone;

        //handle special cases
        if (!movementInfo.Transport.Guid.IsEmpty)
        {
            // We were teleported, skip packets that were broadcast before teleport
            if (movementInfo.Pos.GetExactDist2d(mover.Location) > MapConst.SizeofGrids)
                return;

            if (Math.Abs(movementInfo.Transport.Pos.X) > 75f || Math.Abs(movementInfo.Transport.Pos.Y) > 75f || Math.Abs(movementInfo.Transport.Pos.Z) > 75f)
                return;

            if (!_gridDefines.IsValidMapCoord(movementInfo.Pos.X + movementInfo.Transport.Pos.X,
                                              movementInfo.Pos.Y + movementInfo.Transport.Pos.Y,
                                              movementInfo.Pos.Z + movementInfo.Transport.Pos.Z,
                                              movementInfo.Pos.Orientation + movementInfo.Transport.Pos.Orientation))
                return;

            if (plrMover != null)
            {
                if (plrMover.Transport == null)
                {
                    var go = plrMover.Location.Map.GetGameObject(movementInfo.Transport.Guid);

                    var transport = go?.ToTransportBase();

                    transport?.AddPassenger(plrMover);
                }
                else if (plrMover.Transport.GUID != movementInfo.Transport.Guid)
                {
                    plrMover.Transport.RemovePassenger(plrMover);
                    var go = plrMover.Location.Map.GetGameObject(movementInfo.Transport.Guid);

                    if (go != null)
                    {
                        var transport = go.ToTransportBase();

                        if (transport != null)
                            transport.AddPassenger(plrMover);
                        else
                            movementInfo.ResetTransport();
                    }
                    else
                        movementInfo.ResetTransport();
                }
            }

            if (mover.Transport == null && mover.Vehicle == null)
                movementInfo.Transport.Reset();
        }
        else if (plrMover is { Transport: { } }) // if we were on a transport, leave
            plrMover.Transport.RemovePassenger(plrMover);

        // fall damage generation (ignore in flight case that can be triggered also at lags in moment teleportation to another map).
        if (opcode == ClientOpcodes.MoveFallLand && plrMover is { IsInFlight: false })
            plrMover.HandleFall(movementInfo);

        // interrupt parachutes upon falling or landing in water
        if (opcode is ClientOpcodes.MoveFallLand or ClientOpcodes.MoveStartSwim or ClientOpcodes.MoveSetFly)
            mover.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.LandingOrFlight); // Parachutes

        movementInfo.Guid = mover.GUID;
        movementInfo.Time = AdjustClientMovementTime(movementInfo.Time);
        mover.MovementInfo = movementInfo;

        // Some vehicles allow the passenger to turn by himself
        var vehicle = mover.Vehicle;

        if (vehicle != null)
        {
            var seat = vehicle.GetSeatForPassenger(mover);

            if (seat == null)
                return;

            if (!seat.HasFlag(VehicleSeatFlags.AllowTurning))
                return;

            if (movementInfo.Pos.Orientation != mover.Location.Orientation)
            {
                mover.Location.Orientation = movementInfo.Pos.Orientation;
                mover.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Turning);
            }

            return;
        }

        mover.UpdatePosition(movementInfo.Pos);

        MoveUpdate moveUpdate = new()
        {
            Status = mover.MovementInfo
        };

        mover.SendMessageToSet(moveUpdate, _session.Player);

        if (plrMover == null) // nothing is charmed, or player charmed
            return;

        if (plrMover.IsSitState && movementInfo.HasMovementFlag(MovementFlag.MaskMoving | MovementFlag.MaskTurning))
            plrMover.SetStandState(UnitStandStateType.Stand);

        plrMover.UpdateFallInformationIfNeed(movementInfo, opcode);

        if (movementInfo.Pos.Z < plrMover.Location.Map.GetMinHeight(plrMover.Location.PhaseShift, movementInfo.Pos.X, movementInfo.Pos.Y))
        {
            if (!(plrMover.Battleground != null && plrMover.Battleground.HandlePlayerUnderMap(_session.Player)))
                // NOTE: this is actually called many times while falling
                // even after the player has been teleported away
                // @todo discard movement packets after the player is rooted
                if (plrMover.IsAlive)
                {
                    Log.Logger.Debug($"FALLDAMAGE Below map. Map min height: {plrMover.Location.Map.GetMinHeight(plrMover.Location.PhaseShift, movementInfo.Pos.X, movementInfo.Pos.Y)}, Player debug info:\n{plrMover.GetDebugInfo()}");
                    plrMover.SetPlayerFlag(PlayerFlags.IsOutOfBounds);
                    plrMover.EnvironmentalDamage(EnviromentalDamage.FallToVoid, (uint)_session.Player.MaxHealth);

                    // player can be alive if GM/etc
                    // change the death state to CORPSE to prevent the death timer from
                    // starting in the next player update
                    if (plrMover.IsAlive)
                        plrMover.KillPlayer();
                }
        }
        else
            plrMover.RemovePlayerFlag(PlayerFlags.IsOutOfBounds);

        if (opcode != ClientOpcodes.MoveJump)
            return;

        plrMover.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.Jump); // Mind Control
        _unitCombatHelpers.ProcSkillsAndAuras(plrMover, null, new ProcFlagsInit(ProcFlags.Jump), new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
    }

    [WorldPacketHandler(ClientOpcodes.MoveRemoveMovementForceAck, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveRemoveMovementForceAck(MoveRemoveMovementForceAck moveRemoveMovementForceAck)
    {
        var mover = _session.Player.UnitBeingMoved;
        _session.Player.ValidateMovementInfo(moveRemoveMovementForceAck.Ack.Status);

        // prevent tampered movement data
        if (moveRemoveMovementForceAck.Ack.Status.Guid != mover.GUID)
        {
            Log.Logger.Error($"HandleMoveRemoveMovementForceAck: guid error, expected {mover.GUID}, got {moveRemoveMovementForceAck.Ack.Status.Guid}");

            return;
        }

        moveRemoveMovementForceAck.Ack.Status.Time = AdjustClientMovementTime(moveRemoveMovementForceAck.Ack.Status.Time);

        MoveUpdateRemoveMovementForce updateRemoveMovementForce = new()
        {
            Status = moveRemoveMovementForceAck.Ack.Status,
            TriggerGUID = moveRemoveMovementForceAck.ID
        };

        mover.SendMessageToSet(updateRemoveMovementForce, false);
    }

    [WorldPacketHandler(ClientOpcodes.MoveSetModMovementForceMagnitudeAck, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveSetModMovementForceMagnitudeAck(MovementSpeedAck setModMovementForceMagnitudeAck)
    {
        var mover = _session.Player.UnitBeingMoved;
        _session.Player.ValidateMovementInfo(setModMovementForceMagnitudeAck.Ack.Status);

        // prevent tampered movement data
        if (setModMovementForceMagnitudeAck.Ack.Status.Guid != mover.GUID)
        {
            Log.Logger.Error($"HandleSetModMovementForceMagnitudeAck: guid error, expected {mover.GUID}, got {setModMovementForceMagnitudeAck.Ack.Status.Guid}");

            return;
        }

        // skip all except last
        if (_session.Player.MovementForceModMagnitudeChanges > 0)
        {
            --_session.Player.MovementForceModMagnitudeChanges;

            if (_session.Player.MovementForceModMagnitudeChanges == 0)
            {
                var expectedModMagnitude = 1.0f;
                var movementForces = mover.MovementForces;

                if (movementForces != null)
                    expectedModMagnitude = movementForces.ModMagnitude;

                if (Math.Abs(expectedModMagnitude - setModMovementForceMagnitudeAck.Speed) > 0.01f)
                {
                    Log.Logger.Debug($"Player {_session.Player.GetName()} from account id {_session.Player.Session.AccountId} kicked for incorrect movement force magnitude (must be {expectedModMagnitude} instead {setModMovementForceMagnitudeAck.Speed})");
                    _session.Player.Session.KickPlayer("WorldSession::HandleMoveSetModMovementForceMagnitudeAck Incorrect magnitude");

                    return;
                }
            }
        }

        setModMovementForceMagnitudeAck.Ack.Status.Time = AdjustClientMovementTime(setModMovementForceMagnitudeAck.Ack.Status.Time);

        MoveUpdateSpeed updateModMovementForceMagnitude = new(ServerOpcodes.MoveUpdateModMovementForceMagnitude)
        {
            Status = setModMovementForceMagnitudeAck.Ack.Status,
            Speed = setModMovementForceMagnitudeAck.Speed
        };

        mover.SendMessageToSet(updateModMovementForceMagnitude, false);
    }

    [WorldPacketHandler(ClientOpcodes.MoveSplineDone, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveSplineDoneOpcode(MoveSplineDone moveSplineDone)
    {
        var movementInfo = moveSplineDone.Status;
        _session.Player.ValidateMovementInfo(movementInfo);

        // in taxi flight packet received in 2 case:
        // 1) end taxi path in far (multi-node) flight
        // 2) switch from one map to other in case multim-map taxi path
        // we need process only (1)

        var curDest = _session.Player.Taxi.GetTaxiDestination();

        if (curDest != 0)
        {
            var curDestNode = _taxiNodesRecords.LookupByKey(curDest);

            // far teleport case
            if (curDestNode == null || curDestNode.ContinentID == _session.Player.Location.MapId || _session.Player.MotionMaster.GetCurrentMovementGeneratorType() != MovementGeneratorType.Flight)
                return;

            if (_session.Player.MotionMaster.GetCurrentMovementGenerator() is not FlightPathMovementGenerator flight)
                return;

            // short preparations to continue flight
            flight.SetCurrentNodeAfterTeleport();
            var node = flight.Path[(int)flight.GetCurrentNode()];
            flight.SkipCurrentNode();

            _session.Player.TeleportTo(curDestNode.ContinentID, node.Loc.X, node.Loc.Y, node.Loc.Z, _session.Player.Location.Orientation);

            return;
        }

        // at this point only 1 node is expected (final destination)
        if (_session.Player.Taxi.GetPath().Count != 1)
            return;

        _session.Player.CleanupAfterTaxiFlight();
        _session.Player.SetFallInformation(0, _session.Player.Location.Z);

        if (_session.Player.PvpInfo.IsHostile)
            _session.Player.SpellFactory.CastSpell(2479, true);
    }

    [WorldPacketHandler(ClientOpcodes.MoveTeleportAck, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveTeleportAck(MoveTeleportAck packet)
    {
        var plMover = _session.Player.UnitBeingMoved.AsPlayer;

        if (plMover is not { IsBeingTeleportedNear: true })
            return;

        if (packet.MoverGUID != plMover.GUID)
            return;

        plMover.SetSemaphoreTeleportNear(false);

        var oldZone = plMover.Location.Zone;

        var dest = plMover.TeleportDest;

        plMover.UpdatePosition(dest, true);
        plMover.SetFallInformation(0, _session.Player.Location.Z);

        plMover.UpdateZone(plMover.Location.Zone, plMover.Location.Area);

        // new zone
        if (oldZone != plMover.Location.Zone)
        {
            // honorless target
            if (plMover.PvpInfo.IsHostile)
                plMover.SpellFactory.CastSpell(plMover, 2479, true);

            // in friendly area
            else if (plMover.IsPvP && !plMover.HasPlayerFlag(PlayerFlags.InPVP))
                plMover.UpdatePvP(false);
        }

        // resummon pet
        _session.Player.ResummonPetTemporaryUnSummonedIfAny();

        //lets process all delayed operations on successful teleport
        _session.Player.ProcessDelayedOperations();
    }

    [WorldPacketHandler(ClientOpcodes.MoveTimeSkipped, Processing = PacketProcessing.Inplace)]
    private void HandleMoveTimeSkipped(MoveTimeSkipped moveTimeSkipped)
    {
        var mover = _session.Player.UnitBeingMoved;

        if (mover == null)
        {
            Log.Logger.Warning($"WorldSession.HandleMoveTimeSkipped wrong mover state from the unit moved by {_session.Player.GUID}");

            return;
        }

        // prevent tampered movement data
        if (moveTimeSkipped.MoverGUID != mover.GUID)
        {
            Log.Logger.Warning($"WorldSession.HandleMoveTimeSkipped wrong guid from the unit moved by {_session.Player.GUID}");

            return;
        }

        mover.MovementInfo.Time += moveTimeSkipped.TimeSkipped;

        MoveSkipTime moveSkipTime = new()
        {
            MoverGUID = moveTimeSkipped.MoverGUID,
            TimeSkipped = moveTimeSkipped.TimeSkipped
        };

        mover.SendMessageToSet(moveSkipTime, _session.Player);
    }

    [WorldPacketHandler(ClientOpcodes.WorldPortResponse, Status = SessionStatus.Transfer)]
    private void HandleMoveWorldportAck(WorldPortResponse packet)
    {
        if (packet == null)
            return;

        HandleMoveWorldportAck();
    }

    private void HandleMoveWorldportAck()
    {
        // ignore unexpected far teleports
        if (!_session.Player.IsBeingTeleportedFar)
            return;

        var seamlessTeleport = _session.Player.IsBeingTeleportedSeamlessly;
        _session.Player.SetSemaphoreTeleportFar(false);

        // get the teleport destination
        var loc = _session.Player.TeleportDest;

        // possible errors in the coordinate validity check
        if (!_gridDefines.IsValidMapCoord(loc))
        {
            _session.LogoutPlayer(false);

            return;
        }

        // get the destination map entry, not the current one, this will fix homebind and reset greeting
        var mapEntry = _mapRecords.LookupByKey(loc.MapId);

        // reset instance validity, except if going to an instance inside an instance
        if (!_session.Player.InstanceValid && !mapEntry.IsDungeon())
            _session.Player.InstanceValid = true;

        var oldMap = _session.Player.Location.Map;
        var newMap = _session.Player.TeleportDestInstanceId.HasValue ? _mapManager.FindMap(loc.MapId, _session.Player.TeleportDestInstanceId.Value) : _mapManager.CreateMap(loc.MapId, _session.Player);

        var transportInfo = _session.Player.MovementInfo.Transport;
        var transport = _session.Player.Transport;

        transport?.RemovePassenger(_session.Player);

        if (_session.Player.Location.IsInWorld)
        {
            Log.Logger.Error($"Player (Name {_session.Player.GetName()}) is still in world when teleported from map {oldMap.Id} to new map {loc.MapId}");
            oldMap.RemovePlayerFromMap(_session.Player, false);
        }

        // relocate the player to the teleport destination
        // the CannotEnter checks are done in TeleporTo but conditions may change
        // while the player is in transit, for example the map may get full
        if (newMap == null || newMap.CannotEnter(_session.Player) != null)
        {
            Log.Logger.Error($"Map {loc.MapId} could not be created for {(newMap != null ? newMap.MapName : "Unknown")} ({_session.Player.GUID}), porting player to homebind");
            _session.Player.TeleportTo(_session.Player.Homebind);

            return;
        }

        var z = loc.Z + _session.Player.HoverOffset;
        _session.Player.Location.Relocate(loc.X, loc.Y, z, loc.Orientation);
        _session.Player.SetFallInformation(0, _session.Player.Location.Z);

        _session.Player.Location.ResetMap();
        _session.Player.Location.Map = newMap;
        _session.Player.CheckAddToMap();

        ResumeToken resumeToken = new()
        {
            SequenceIndex = _session.Player.MovementCounter,
            Reason = seamlessTeleport ? 2 : 1u
        };

        _session.SendPacket(resumeToken);

        if (!seamlessTeleport)
            _session.Player.SendInitialPacketsBeforeAddToMap();

        // move player between transport copies on each map
        var newTransport = newMap.GetTransport(transportInfo.Guid);

        if (newTransport != null)
        {
            _session.Player.MovementInfo.Transport = transportInfo;
            newTransport.AddPassenger(_session.Player);
        }

        if (!_session.Player.Location.Map.AddPlayerToMap(_session.Player, !seamlessTeleport))
        {
            Log.Logger.Error($"WORLD: failed to teleport player {_session.Player.GetName()} ({_session.Player.GUID}) to map {loc.MapId} ({newMap.MapName}) because of unknown reason!");
            _session.Player.Location.ResetMap();
            _session.Player.Location.Map = oldMap;
            _session.Player.CheckAddToMap();
            _session.Player.TeleportTo(_session.Player.Homebind);

            return;
        }

        // Battleground state prepare (in case join to BG), at relogin/tele player not invited
        // only add to bg group and object, if the player was invited (else he entered through command)
        if (_session.Player.InBattleground)
        {
            // cleanup setting if outdated
            if (!mapEntry.IsBattlegroundOrArena())
            {
                // We're not in BG
                _session.Player.SetBattlegroundId(0, BattlegroundTypeId.None);
                // reset destination bg team
                _session.Player.SetBgTeam(0);
            }
            // join to bg case
            else
            {
                if (_session.Player.Battleground != null)
                    if (_session.Player.IsInvitedForBattlegroundInstance(_session.Player.BattlegroundId))
                        _session.Player.Battleground.AddPlayer(_session.Player);
            }
        }

        if (!seamlessTeleport)
            _session.Player.SendInitialPacketsAfterAddToMap();
        else
        {
            _session.Player.UpdateVisibilityForPlayer();
            var garrison = _session.Player.Garrison;

            garrison?.SendRemoteInfo();
        }

        // flight fast teleport case
        if (_session.Player.IsInFlight)
        {
            if (!_session.Player.InBattleground)
            {
                if (seamlessTeleport)
                    return;

                // short preparations to continue flight
                var movementGenerator = _session.Player.MotionMaster.GetCurrentMovementGenerator();
                movementGenerator.Initialize(_session.Player);

                return;
            }

            // Battlegroundstate prepare, stop flight
            _session.Player.FinishTaxiFlight();
        }

        if (!_session.Player.IsAlive && _session.Player.TeleportOptions.HasAnyFlag(TeleportToOptions.ReviveAtTeleport))
            _session.Player.ResurrectPlayer(0.5f);

        // resurrect character at enter into instance where his corpse exist after add to map
        if (mapEntry.IsDungeon() && !_session.Player.IsAlive)
            if (_session.Player.CorpseLocation.MapId == mapEntry.Id)
            {
                _session.Player.ResurrectPlayer(0.5f);
                _session.Player.SpawnCorpseBones();
            }

        if (mapEntry.IsDungeon())
        {
            // check if this instance has a reset time and send it to player if so
            MapDb2Entries entries = new(mapEntry.Id, newMap.DifficultyID, _cliDB, _db2Manager);

            if (entries.MapDifficulty.HasResetSchedule())
            {
                RaidInstanceMessage raidInstanceMessage = new()
                {
                    Type = InstanceResetWarningType.Welcome,
                    MapID = mapEntry.Id,
                    DifficultyID = newMap.DifficultyID
                };

                var playerLock = _instanceLockManager.FindActiveInstanceLock(_session.Player.GUID, entries);

                if (playerLock != null)
                {
                    raidInstanceMessage.Locked = !playerLock.IsExpired();
                    raidInstanceMessage.Extended = playerLock.IsExtended();
                }
                else
                {
                    raidInstanceMessage.Locked = false;
                    raidInstanceMessage.Extended = false;
                }

                _session.SendPacket(raidInstanceMessage);
            }

            // check if instance is valid
            if (!_session.Player.CheckInstanceValidity(false))
                _session.Player.InstanceValid = false;
        }

        // update zone immediately, otherwise leave channel will cause crash in mtmap
        _session.Player.UpdateZone(_session.Player.Location.Zone, _session.Player.Location.Area);

        // honorless target
        if (_session.Player.PvpInfo.IsHostile)
            _session.Player.SpellFactory.CastSpell(2479, true);

        // in friendly area
        else if (_session.Player.IsPvP && !_session.Player.HasPlayerFlag(PlayerFlags.InPVP))
            _session.Player.UpdatePvP(false);

        // resummon pet
        _session.Player.ResummonPetTemporaryUnSummonedIfAny();

        //lets process all delayed operations on successful teleport
        _session.Player.ProcessDelayedOperations();
    }

    [WorldPacketHandler(ClientOpcodes.SetActiveMover)]
    private void HandleSetActiveMover(SetActiveMover packet)
    {
        if (!_session.Player.Location.IsInWorld)
            return;

        if (_session.Player.UnitBeingMoved.GUID != packet.ActiveMover)
            Log.Logger.Error("HandleSetActiveMover: incorrect mover guid: mover is {0} and should be {1},", packet.ActiveMover.ToString(), _session.Player.UnitBeingMoved.GUID.ToString());
    }

    [WorldPacketHandler(ClientOpcodes.MoveSetCollisionHeightAck, Processing = PacketProcessing.ThreadSafe)]
    private void HandleSetCollisionHeightAck(MoveSetCollisionHeightAck packet)
    {
        if (packet == null)
            return;

        _session.Player.ValidateMovementInfo(packet.Data.Status);
    }

    [WorldPacketHandler(ClientOpcodes.SuspendTokenResponse, Status = SessionStatus.Transfer)]
    private void HandleSuspendTokenResponse(SuspendTokenResponse suspendTokenResponse)
    {
        if (suspendTokenResponse == null || !_session.Player.IsBeingTeleportedFar)
            return;

        var loc = _session.Player.TeleportDest;

        if (_mapRecords.LookupByKey(loc.MapId).IsDungeon())
        {
            _session.SendPacket(new UpdateLastInstance()
            {
                MapID = loc.MapId
            });
        }

        _session.SendPacket(new NewWorld()
        {
            MapID = loc.MapId,
            Loc =
            {
                Pos = loc
            },
            Reason = (uint)(!_session.Player.IsBeingTeleportedSeamlessly ? NewWorldReason.Normal : NewWorldReason.Seamless)
        });

        if (_session.Player.IsBeingTeleportedSeamlessly)
            HandleMoveWorldportAck();
    }

    [WorldPacketHandler(ClientOpcodes.TimeSyncResponse, Processing = PacketProcessing.ThreadSafe)]
    private void HandleTimeSyncResponse(TimeSyncResponse timeSyncResponse)
    {
        if (!_pendingTimeSyncRequests.ContainsKey(timeSyncResponse.SequenceIndex))
            return;

        var serverTimeAtSent = _pendingTimeSyncRequests.LookupByKey(timeSyncResponse.SequenceIndex);
        _pendingTimeSyncRequests.Remove(timeSyncResponse.SequenceIndex);

        // time it took for the request to travel to the client, for the client to process it and reply and for response to travel back to the server.
        // we are going to make 2 assumptions:
        // 1) we assume that the request processing time equals 0.
        // 2) we assume that the packet took as much time to travel from server to client than it took to travel from client to server.
        var roundTripDuration = Time.GetMSTimeDiff(serverTimeAtSent, timeSyncResponse.GetReceivedTime());
        var lagDelay = roundTripDuration / 2;

        /*
        clockDelta = serverTime - clientTime
        where
        serverTime: time that was displayed on the clock of the SERVER at the moment when the client processed the SMSG_TIME_SYNC_REQUEST packet.
        clientTime:  time that was displayed on the clock of the CLIENT at the moment when the client processed the SMSG_TIME_SYNC_REQUEST packet.

        Once clockDelta has been computed, we can compute the time of an event on server clock when we know the time of that same event on the client clock,
        using the following relation:
        serverTime = clockDelta + clientTime
        */
        var clockDelta = serverTimeAtSent + lagDelay - (long)timeSyncResponse.ClientTime;
        _timeSyncClockDeltaQueue.PushFront(Tuple.Create(clockDelta, roundTripDuration));
        ComputeNewClockDelta();
    }
}