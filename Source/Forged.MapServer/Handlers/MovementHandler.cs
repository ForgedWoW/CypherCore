// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Movement.Generators;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Instance;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Movement;
using Forged.MapServer.Spells;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

namespace Forged.MapServer.Handlers;

public class MovementHandler : IWorldSessionHandler
{
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

    private void HandleMovementOpcode(ClientOpcodes opcode, MovementInfo movementInfo)
    {
        var mover = Player.UnitBeingMoved;
        var plrMover = mover.AsPlayer;

        if (plrMover && plrMover.IsBeingTeleported)
            return;

        Player.ValidateMovementInfo(movementInfo);

        if (movementInfo.Guid != mover.GUID)
        {
            Log.Logger.Error("HandleMovementOpcodes: guid error");

            return;
        }

        if (!movementInfo.Pos.IsPositionValid)
            return;


        if (!mover.MoveSpline.Finalized())
            return;

        // stop some emotes at player move
        if (plrMover && (plrMover.EmoteState != 0))
            plrMover.EmoteState = Emote.OneshotNone;

        //handle special cases
        if (!movementInfo.Transport.Guid.IsEmpty)
        {
            // We were teleported, skip packets that were broadcast before teleport
            if (movementInfo.Pos.GetExactDist2d(mover.Location) > MapConst.SizeofGrids)
                return;

            if (Math.Abs(movementInfo.Transport.Pos.X) > 75f || Math.Abs(movementInfo.Transport.Pos.Y) > 75f || Math.Abs(movementInfo.Transport.Pos.Z) > 75f)
                return;

            if (!GridDefines.IsValidMapCoord(movementInfo.Pos.X + movementInfo.Transport.Pos.X,
                                             movementInfo.Pos.Y + movementInfo.Transport.Pos.Y,
                                             movementInfo.Pos.Z + movementInfo.Transport.Pos.Z,
                                             movementInfo.Pos.Orientation + movementInfo.Transport.Pos.Orientation))
                return;

            if (plrMover)
            {
                if (plrMover.Transport == null)
                {
                    var go = plrMover.Map.GetGameObject(movementInfo.Transport.Guid);

                    if (go != null)
                    {
                        var transport = go.ToTransportBase();

                        if (transport != null)
                            transport.AddPassenger(plrMover);
                    }
                }
                else if (plrMover.Transport.GetTransportGUID() != movementInfo.Transport.Guid)
                {
                    plrMover.Transport.RemovePassenger(plrMover);
                    var go = plrMover.Map.GetGameObject(movementInfo.Transport.Guid);

                    if (go != null)
                    {
                        var transport = go.ToTransportBase();

                        if (transport != null)
                            transport.AddPassenger(plrMover);
                        else
                            movementInfo.ResetTransport();
                    }
                    else
                    {
                        movementInfo.ResetTransport();
                    }
                }
            }

            if (mover.Transport == null && !mover.Vehicle1)
                movementInfo.Transport.Reset();
        }
        else if (plrMover && plrMover.Transport != null) // if we were on a transport, leave
        {
            plrMover.Transport.RemovePassenger(plrMover);
        }

        // fall damage generation (ignore in flight case that can be triggered also at lags in moment teleportation to another map).
        if (opcode == ClientOpcodes.MoveFallLand && plrMover && !plrMover.IsInFlight)
            plrMover.HandleFall(movementInfo);

        // interrupt parachutes upon falling or landing in water
        if (opcode == ClientOpcodes.MoveFallLand || opcode == ClientOpcodes.MoveStartSwim || opcode == ClientOpcodes.MoveSetFly)
            mover.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.LandingOrFlight); // Parachutes

        movementInfo.Guid = mover.GUID;
        movementInfo.Time = AdjustClientMovementTime(movementInfo.Time);
        mover.MovementInfo = movementInfo;

        // Some vehicles allow the passenger to turn by himself
        var vehicle = mover.Vehicle1;

        if (vehicle)
        {
            var seat = vehicle.GetSeatForPassenger(mover);

            if (seat != null)
                if (seat.HasFlag(VehicleSeatFlags.AllowTurning))
                    if (movementInfo.Pos.Orientation != mover.Location.Orientation)
                    {
                        mover.Location.Orientation = movementInfo.Pos.Orientation;
                        mover.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Turning);
                    }

            return;
        }

        mover.UpdatePosition(movementInfo.Pos);

        MoveUpdate moveUpdate = new();
        moveUpdate.Status = mover.MovementInfo;
        mover.SendMessageToSet(moveUpdate, Player);

        if (plrMover) // nothing is charmed, or player charmed
        {
            if (plrMover.IsSitState && movementInfo.HasMovementFlag(MovementFlag.MaskMoving | MovementFlag.MaskTurning))
                plrMover.SetStandState(UnitStandStateType.Stand);

            plrMover.UpdateFallInformationIfNeed(movementInfo, opcode);

            if (movementInfo.Pos.Z < plrMover.Map.GetMinHeight(plrMover.PhaseShift, movementInfo.Pos.X, movementInfo.Pos.Y))
            {
                if (!(plrMover.Battleground && plrMover.Battleground.HandlePlayerUnderMap(Player)))
                    // NOTE: this is actually called many times while falling
                    // even after the player has been teleported away
                    // @todo discard movement packets after the player is rooted
                    if (plrMover.IsAlive)
                    {
                        Log.Logger.Debug($"FALLDAMAGE Below map. Map min height: {plrMover.Map.GetMinHeight(plrMover.PhaseShift, movementInfo.Pos.X, movementInfo.Pos.Y)}, Player debug info:\n{plrMover.GetDebugInfo()}");
                        plrMover.SetPlayerFlag(PlayerFlags.IsOutOfBounds);
                        plrMover.EnvironmentalDamage(EnviromentalDamage.FallToVoid, (uint)Player.MaxHealth);

                        // player can be alive if GM/etc
                        // change the death state to CORPSE to prevent the death timer from
                        // starting in the next player update
                        if (plrMover.IsAlive)
                            plrMover.KillPlayer();
                    }
            }
            else
            {
                plrMover.RemovePlayerFlag(PlayerFlags.IsOutOfBounds);
            }

            if (opcode == ClientOpcodes.MoveJump)
            {
                plrMover.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags2.Jump); // Mind Control
                Unit.ProcSkillsAndAuras(plrMover, null, new ProcFlagsInit(ProcFlags.Jump), new ProcFlagsInit(ProcFlags.None), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
            }
        }
    }

    [WorldPacketHandler(ClientOpcodes.WorldPortResponse, Status = SessionStatus.Transfer)]
    private void HandleMoveWorldportAck(WorldPortResponse packet)
    {
        HandleMoveWorldportAck();
    }

    private void HandleMoveWorldportAck()
    {
        var player = Player;

        // ignore unexpected far teleports
        if (!player.IsBeingTeleportedFar)
            return;

        var seamlessTeleport = player.IsBeingTeleportedSeamlessly;
        player.SetSemaphoreTeleportFar(false);

        // get the teleport destination
        var loc = player.TeleportDest;

        // possible errors in the coordinate validity check
        if (!GridDefines.IsValidMapCoord((WorldLocation)loc))
        {
            LogoutPlayer(false);

            return;
        }

        // get the destination map entry, not the current one, this will fix homebind and reset greeting
        var mapEntry = CliDB.MapStorage.LookupByKey(loc.MapId);

        // reset instance validity, except if going to an instance inside an instance
        if (!player.InstanceValid && !mapEntry.IsDungeon())
            player.InstanceValid = true;

        var oldMap = player.Map;
        var newMap = Player.TeleportDestInstanceId.HasValue ? Global.MapMgr.FindMap(loc.MapId, Player.TeleportDestInstanceId.Value) : Global.MapMgr.CreateMap(loc.MapId, Player);

        var transportInfo = player.MovementInfo.Transport;
        var transport = player.Transport;

        if (transport != null)
            transport.RemovePassenger(player);

        if (player.IsInWorld)
        {
            Log.Logger.Error($"Player (Name {player.GetName()}) is still in world when teleported from map {oldMap.Id} to new map {loc.MapId}");
            oldMap.RemovePlayerFromMap(player, false);
        }

        // relocate the player to the teleport destination
        // the CannotEnter checks are done in TeleporTo but conditions may change
        // while the player is in transit, for example the map may get full
        if (newMap == null || newMap.CannotEnter(player) != null)
        {
            Log.Logger.Error($"Map {loc.MapId} could not be created for {(newMap ? newMap.MapName : "Unknown")} ({player.GUID}), porting player to homebind");
            player.TeleportTo(player.Homebind);

            return;
        }

        var z = loc.Z + player.HoverOffset;
        player.Location.Relocate(loc.X, loc.Y, z, loc.Orientation);
        player.SetFallInformation(0, player.Location.Z);

        player.ResetMap();
        player.Location.Map = newMap;
        player.CheckAddToMap();

        ResumeToken resumeToken = new();
        resumeToken.SequenceIndex = player.MovementCounter;
        resumeToken.Reason = seamlessTeleport ? 2 : 1u;
        SendPacket(resumeToken);

        if (!seamlessTeleport)
            player.SendInitialPacketsBeforeAddToMap();

        // move player between transport copies on each map
        var newTransport = newMap.GetTransport(transportInfo.Guid);

        if (newTransport != null)
        {
            player.MovementInfo.Transport = transportInfo;
            newTransport.AddPassenger(player);
        }

        if (!player.Map.AddPlayerToMap(player, !seamlessTeleport))
        {
            Log.Logger.Error($"WORLD: failed to teleport player {player.GetName()} ({player.GUID}) to map {loc.MapId} ({(newMap ? newMap.MapName : "Unknown")}) because of unknown reason!");
            player.ResetMap();
            player.Location.Map = oldMap;
            player.CheckAddToMap();
            player.TeleportTo(player.Homebind);

            return;
        }

        // Battleground state prepare (in case join to BG), at relogin/tele player not invited
        // only add to bg group and object, if the player was invited (else he entered through command)
        if (player.InBattleground)
        {
            // cleanup setting if outdated
            if (!mapEntry.IsBattlegroundOrArena())
            {
                // We're not in BG
                player.SetBattlegroundId(0, BattlegroundTypeId.None);
                // reset destination bg team
                player.SetBgTeam(0);
            }
            // join to bg case
            else
            {
                var bg = player.Battleground;

                if (bg)
                    if (player.IsInvitedForBattlegroundInstance(player.BattlegroundId))
                        bg.AddPlayer(player);
            }
        }

        if (!seamlessTeleport)
        {
            player.SendInitialPacketsAfterAddToMap();
        }
        else
        {
            player.UpdateVisibilityForPlayer();
            var garrison = player.Garrison;

            if (garrison != null)
                garrison.SendRemoteInfo();
        }

        // flight fast teleport case
        if (player.IsInFlight)
        {
            if (!player.InBattleground)
            {
                if (!seamlessTeleport)
                {
                    // short preparations to continue flight
                    var movementGenerator = player.MotionMaster.GetCurrentMovementGenerator();
                    movementGenerator.Initialize(player);
                }

                return;
            }

            // Battlegroundstate prepare, stop flight
            player.FinishTaxiFlight();
        }

        if (!player.IsAlive && Extensions.HasAnyFlag(player.TeleportOptions, TeleportToOptions.ReviveAtTeleport))
            player.ResurrectPlayer(0.5f);

        // resurrect character at enter into instance where his corpse exist after add to map
        if (mapEntry.IsDungeon() && !player.IsAlive)
            if (player.CorpseLocation.MapId == mapEntry.Id)
            {
                player.ResurrectPlayer(0.5f, false);
                player.SpawnCorpseBones();
            }

        if (mapEntry.IsDungeon())
        {
            // check if this instance has a reset time and send it to player if so
            MapDb2Entries entries = new(mapEntry.Id, newMap.DifficultyID);

            if (entries.MapDifficulty.HasResetSchedule())
            {
                RaidInstanceMessage raidInstanceMessage = new();
                raidInstanceMessage.Type = InstanceResetWarningType.Welcome;
                raidInstanceMessage.MapID = mapEntry.Id;
                raidInstanceMessage.DifficultyID = newMap.DifficultyID;

                var playerLock = Global.InstanceLockMgr.FindActiveInstanceLock(Player.GUID, entries);

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

                SendPacket(raidInstanceMessage);
            }

            // check if instance is valid
            if (!player.CheckInstanceValidity(false))
                player.InstanceValid = false;
        }

        // update zone immediately, otherwise leave channel will cause crash in mtmap
        player.GetZoneAndAreaId(out var newzone, out var newarea);
        player.UpdateZone(newzone, newarea);

        // honorless target
        if (player.PvpInfo.IsHostile)
            player.CastSpell(player, 2479, true);

        // in friendly area
        else if (player.IsPvP && !player.HasPlayerFlag(PlayerFlags.InPVP))
            player.UpdatePvP(false, false);

        // resummon pet
        player.ResummonPetTemporaryUnSummonedIfAny();

        //lets process all delayed operations on successful teleport
        player.ProcessDelayedOperations();
    }

    [WorldPacketHandler(ClientOpcodes.SuspendTokenResponse, Status = SessionStatus.Transfer)]
    private void HandleSuspendTokenResponse(SuspendTokenResponse suspendTokenResponse)
    {
        if (!_player.IsBeingTeleportedFar)
            return;

        var loc = Player.TeleportDest;

        if (CliDB.MapStorage.LookupByKey(loc.MapId).IsDungeon())
        {
            UpdateLastInstance updateLastInstance = new();
            updateLastInstance.MapID = loc.MapId;
            SendPacket(updateLastInstance);
        }

        NewWorld packet = new();
        packet.MapID = loc.MapId;
        packet.Loc.Pos = loc;
        packet.Reason = (uint)(!_player.IsBeingTeleportedSeamlessly ? NewWorldReason.Normal : NewWorldReason.Seamless);
        SendPacket(packet);

        if (_player.IsBeingTeleportedSeamlessly)
            HandleMoveWorldportAck();
    }

    [WorldPacketHandler(ClientOpcodes.MoveTeleportAck, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveTeleportAck(MoveTeleportAck packet)
    {
        var plMover = Player.UnitBeingMoved.AsPlayer;

        if (!plMover || !plMover.IsBeingTeleportedNear)
            return;

        if (packet.MoverGUID != plMover.GUID)
            return;

        plMover.SetSemaphoreTeleportNear(false);

        var old_zone = plMover.Zone;

        var dest = plMover.TeleportDest;

        plMover.UpdatePosition(dest, true);
        plMover.SetFallInformation(0, Player.Location.Z);

        plMover.GetZoneAndAreaId(out var newzone, out var newarea);
        plMover.UpdateZone(newzone, newarea);

        // new zone
        if (old_zone != newzone)
        {
            // honorless target
            if (plMover.PvpInfo.IsHostile)
                plMover.CastSpell(plMover, 2479, true);

            // in friendly area
            else if (plMover.IsPvP && !plMover.HasPlayerFlag(PlayerFlags.InPVP))
                plMover.UpdatePvP(false, false);
        }

        // resummon pet
        Player.ResummonPetTemporaryUnSummonedIfAny();

        //lets process all delayed operations on successful teleport
        Player.ProcessDelayedOperations();
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
        Player.ValidateMovementInfo(packet.Ack.Status);

        // now can skip not our packet
        if (Player.GUID != packet.Ack.Status.Guid)
            return;

        /*----------------*/
        // client ACK send one packet for mounted/run case and need skip all except last from its
        // in other cases anti-cheat check can be fail in false case
        UnitMoveType move_type;

        var opcode = packet.GetOpcode();

        switch (opcode)
        {
            case ClientOpcodes.MoveForceWalkSpeedChangeAck:
                move_type = UnitMoveType.Walk;

                break;
            case ClientOpcodes.MoveForceRunSpeedChangeAck:
                move_type = UnitMoveType.Run;

                break;
            case ClientOpcodes.MoveForceRunBackSpeedChangeAck:
                move_type = UnitMoveType.RunBack;

                break;
            case ClientOpcodes.MoveForceSwimSpeedChangeAck:
                move_type = UnitMoveType.Swim;

                break;
            case ClientOpcodes.MoveForceSwimBackSpeedChangeAck:
                move_type = UnitMoveType.SwimBack;

                break;
            case ClientOpcodes.MoveForceTurnRateChangeAck:
                move_type = UnitMoveType.TurnRate;

                break;
            case ClientOpcodes.MoveForceFlightSpeedChangeAck:
                move_type = UnitMoveType.Flight;

                break;
            case ClientOpcodes.MoveForceFlightBackSpeedChangeAck:
                move_type = UnitMoveType.FlightBack;

                break;
            case ClientOpcodes.MoveForcePitchRateChangeAck:
                move_type = UnitMoveType.PitchRate;

                break;
            default:
                Log.Logger.Error("WorldSession.HandleForceSpeedChangeAck: Unknown move type opcode: {0}", opcode);

                return;
        }

        // skip all forced speed changes except last and unexpected
        // in run/mounted case used one ACK and it must be skipped. m_forced_speed_changes[MOVE_RUN] store both.
        if (Player.ForcedSpeedChanges[(int)move_type] > 0)
        {
            --Player.ForcedSpeedChanges[(int)move_type];

            if (Player.ForcedSpeedChanges[(int)move_type] > 0)
                return;
        }

        if (Player.Transport == null && Math.Abs((float)(Player.GetSpeed(move_type) - packet.Speed)) > 0.01f)
        {
            if (Player.GetSpeed(move_type) > packet.Speed) // must be greater - just correct
            {
                Log.Logger.Error("{0}SpeedChange player {1} is NOT correct (must be {2} instead {3}), force set to correct value",
                                 move_type,
                                 Player.GetName(),
                                 Player.GetSpeed(move_type),
                                 packet.Speed);

                Player.SetSpeedRate(move_type, Player.GetSpeedRate(move_type));
            }
            else // must be lesser - cheating
            {
                Log.Logger.Debug("Player {0} from account id {1} kicked for incorrect speed (must be {2} instead {3})",
                                 Player.GetName(),
                                 Player.Session.AccountId,
                                 Player.GetSpeed(move_type),
                                 packet.Speed);

                Player.Session.KickPlayer("WorldSession::HandleForceSpeedChangeAck Incorrect speed");
            }
        }
    }

    [WorldPacketHandler(ClientOpcodes.SetActiveMover)]
    private void HandleSetActiveMover(SetActiveMover packet)
    {
        if (Player.IsInWorld)
            if (_player.UnitBeingMoved.GUID != packet.ActiveMover)
                Log.Logger.Error("HandleSetActiveMover: incorrect mover guid: mover is {0} and should be {1},", packet.ActiveMover.ToString(), _player.UnitBeingMoved.GUID.ToString());
    }

    [WorldPacketHandler(ClientOpcodes.MoveKnockBackAck, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveKnockBackAck(MoveKnockBackAck movementAck)
    {
        Player.ValidateMovementInfo(movementAck.Ack.Status);

        if (Player.UnitBeingMoved.GUID != movementAck.Ack.Status.Guid)
            return;

        movementAck.Ack.Status.Time = AdjustClientMovementTime(movementAck.Ack.Status.Time);
        Player.MovementInfo = movementAck.Ack.Status;

        MoveUpdateKnockBack updateKnockBack = new();
        updateKnockBack.Status = Player.MovementInfo;
        Player.SendMessageToSet(updateKnockBack, false);
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
        Player.ValidateMovementInfo(movementAck.Ack.Status);
    }

    [WorldPacketHandler(ClientOpcodes.MoveSetCollisionHeightAck, Processing = PacketProcessing.ThreadSafe)]
    private void HandleSetCollisionHeightAck(MoveSetCollisionHeightAck packet)
    {
        Player.ValidateMovementInfo(packet.Data.Status);
    }

    [WorldPacketHandler(ClientOpcodes.MoveApplyMovementForceAck, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveApplyMovementForceAck(MoveApplyMovementForceAck moveApplyMovementForceAck)
    {
        var mover = _player.UnitBeingMoved;
        _player.ValidateMovementInfo(moveApplyMovementForceAck.Ack.Status);

        // prevent tampered movement data
        if (moveApplyMovementForceAck.Ack.Status.Guid != mover.GUID)
        {
            Log.Logger.Error($"HandleMoveApplyMovementForceAck: guid error, expected {mover.GUID}, got {moveApplyMovementForceAck.Ack.Status.Guid}");

            return;
        }

        moveApplyMovementForceAck.Ack.Status.Time = AdjustClientMovementTime(moveApplyMovementForceAck.Ack.Status.Time);

        MoveUpdateApplyMovementForce updateApplyMovementForce = new();
        updateApplyMovementForce.Status = moveApplyMovementForceAck.Ack.Status;
        updateApplyMovementForce.Force = moveApplyMovementForceAck.Force;
        mover.SendMessageToSet(updateApplyMovementForce, false);
    }

    [WorldPacketHandler(ClientOpcodes.MoveRemoveMovementForceAck, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveRemoveMovementForceAck(MoveRemoveMovementForceAck moveRemoveMovementForceAck)
    {
        var mover = _player.UnitBeingMoved;
        _player.ValidateMovementInfo(moveRemoveMovementForceAck.Ack.Status);

        // prevent tampered movement data
        if (moveRemoveMovementForceAck.Ack.Status.Guid != mover.GUID)
        {
            Log.Logger.Error($"HandleMoveRemoveMovementForceAck: guid error, expected {mover.GUID}, got {moveRemoveMovementForceAck.Ack.Status.Guid}");

            return;
        }

        moveRemoveMovementForceAck.Ack.Status.Time = AdjustClientMovementTime(moveRemoveMovementForceAck.Ack.Status.Time);

        MoveUpdateRemoveMovementForce updateRemoveMovementForce = new();
        updateRemoveMovementForce.Status = moveRemoveMovementForceAck.Ack.Status;
        updateRemoveMovementForce.TriggerGUID = moveRemoveMovementForceAck.ID;
        mover.SendMessageToSet(updateRemoveMovementForce, false);
    }

    [WorldPacketHandler(ClientOpcodes.MoveSetModMovementForceMagnitudeAck, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveSetModMovementForceMagnitudeAck(MovementSpeedAck setModMovementForceMagnitudeAck)
    {
        var mover = _player.UnitBeingMoved;
        _player.ValidateMovementInfo(setModMovementForceMagnitudeAck.Ack.Status);

        // prevent tampered movement data
        if (setModMovementForceMagnitudeAck.Ack.Status.Guid != mover.GUID)
        {
            Log.Logger.Error($"HandleSetModMovementForceMagnitudeAck: guid error, expected {mover.GUID}, got {setModMovementForceMagnitudeAck.Ack.Status.Guid}");

            return;
        }

        // skip all except last
        if (_player.MovementForceModMagnitudeChanges > 0)
        {
            --_player.MovementForceModMagnitudeChanges;

            if (_player.MovementForceModMagnitudeChanges == 0)
            {
                var expectedModMagnitude = 1.0f;
                var movementForces = mover.MovementForces;

                if (movementForces != null)
                    expectedModMagnitude = movementForces.ModMagnitude;

                if (Math.Abs(expectedModMagnitude - setModMovementForceMagnitudeAck.Speed) > 0.01f)
                {
                    Log.Logger.Debug($"Player {_player.GetName()} from account id {_player.Session.AccountId} kicked for incorrect movement force magnitude (must be {expectedModMagnitude} instead {setModMovementForceMagnitudeAck.Speed})");
                    _player.Session.KickPlayer("WorldSession::HandleMoveSetModMovementForceMagnitudeAck Incorrect magnitude");

                    return;
                }
            }
        }

        setModMovementForceMagnitudeAck.Ack.Status.Time = AdjustClientMovementTime(setModMovementForceMagnitudeAck.Ack.Status.Time);

        MoveUpdateSpeed updateModMovementForceMagnitude = new(ServerOpcodes.MoveUpdateModMovementForceMagnitude);
        updateModMovementForceMagnitude.Status = setModMovementForceMagnitudeAck.Ack.Status;
        updateModMovementForceMagnitude.Speed = setModMovementForceMagnitudeAck.Speed;
        mover.SendMessageToSet(updateModMovementForceMagnitude, false);
    }

    [WorldPacketHandler(ClientOpcodes.MoveTimeSkipped, Processing = PacketProcessing.Inplace)]
    private void HandleMoveTimeSkipped(MoveTimeSkipped moveTimeSkipped)
    {
        var mover = Player.UnitBeingMoved;

        if (mover == null)
        {
            Log.Logger.Warning($"WorldSession.HandleMoveTimeSkipped wrong mover state from the unit moved by {Player.GUID}");

            return;
        }

        // prevent tampered movement data
        if (moveTimeSkipped.MoverGUID != mover.GUID)
        {
            Log.Logger.Warning($"WorldSession.HandleMoveTimeSkipped wrong guid from the unit moved by {Player.GUID}");

            return;
        }

        mover.MovementInfo.Time += moveTimeSkipped.TimeSkipped;

        MoveSkipTime moveSkipTime = new();
        moveSkipTime.MoverGUID = moveTimeSkipped.MoverGUID;
        moveSkipTime.TimeSkipped = moveTimeSkipped.TimeSkipped;
        mover.SendMessageToSet(moveSkipTime, _player);
    }

    [WorldPacketHandler(ClientOpcodes.MoveSplineDone, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveSplineDoneOpcode(MoveSplineDone moveSplineDone)
    {
        var movementInfo = moveSplineDone.Status;
        _player.ValidateMovementInfo(movementInfo);

        // in taxi flight packet received in 2 case:
        // 1) end taxi path in far (multi-node) flight
        // 2) switch from one map to other in case multim-map taxi path
        // we need process only (1)

        var curDest = Player.Taxi.GetTaxiDestination();

        if (curDest != 0)
        {
            var curDestNode = CliDB.TaxiNodesStorage.LookupByKey(curDest);

            // far teleport case
            if (curDestNode != null && curDestNode.ContinentID != Player.Location.MapId && Player.MotionMaster.GetCurrentMovementGeneratorType() == MovementGeneratorType.Flight)
            {
                var flight = Player.MotionMaster.GetCurrentMovementGenerator() as FlightPathMovementGenerator;

                if (flight != null)
                {
                    // short preparations to continue flight
                    flight.SetCurrentNodeAfterTeleport();
                    var node = flight.GetPath()[(int)flight.GetCurrentNode()];
                    flight.SkipCurrentNode();

                    Player.TeleportTo(curDestNode.ContinentID, node.Loc.X, node.Loc.Y, node.Loc.Z, Player.Location.Orientation);
                }
            }

            return;
        }

        // at this point only 1 node is expected (final destination)
        if (Player.Taxi.GetPath().Count != 1)
            return;

        Player.CleanupAfterTaxiFlight();
        Player.SetFallInformation(0, Player.Location.Z);

        if (Player.PvpInfo.IsHostile)
            Player.CastSpell(Player, 2479, true);
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
        var clockDelta = (long)(serverTimeAtSent + lagDelay) - (long)timeSyncResponse.ClientTime;
        _timeSyncClockDeltaQueue.PushFront(Tuple.Create(clockDelta, roundTripDuration));
        ComputeNewClockDelta();
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
            var meanClockDelta = (long)(Math.Round(clockDeltasAfterFiltering.Average()));

            if (Math.Abs(meanClockDelta - _timeSyncClockDelta) > 25)
                _timeSyncClockDelta = meanClockDelta;
        }
        else if (_timeSyncClockDelta == 0)
        {
            var back = _timeSyncClockDeltaQueue.Back();
            _timeSyncClockDelta = back.Item1;
        }
    }

    [WorldPacketHandler(ClientOpcodes.MoveInitActiveMoverComplete, Processing = PacketProcessing.ThreadSafe)]
    private void HandleMoveInitActiveMoverComplete(MoveInitActiveMoverComplete moveInitActiveMoverComplete)
    {
        _player.SetPlayerLocalFlag(PlayerLocalFlags.OverrideTransportServerTime);
        _player.SetTransportServerTime((int)(GameTime.GetGameTimeMS() - moveInitActiveMoverComplete.Ticks));

        _player.UpdateObjectVisibility(false);
    }
}