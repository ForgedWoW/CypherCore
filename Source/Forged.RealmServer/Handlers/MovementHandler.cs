// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Maps;
using Forged.RealmServer.Maps.Grids;
using Forged.RealmServer.Movement;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Forged.RealmServer.Spells;

namespace Forged.RealmServer;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.MoveStartAscend, Processing = PacketProcessing.ThreadSafe)]
	[WorldPacketHandler(ClientOpcodes.MoveStop, Processing = PacketProcessing.ThreadSafe)]
	[WorldPacketHandler(ClientOpcodes.MoveStopAscend, Processing = PacketProcessing.ThreadSafe)]
	void HandleMovement(ClientPlayerMovement packet)
	{
		HandleMovementOpcode(packet.GetOpcode(), packet.Status);
	}

	void HandleMovementOpcode(ClientOpcodes opcode, MovementInfo movementInfo)
	{
		var mover = Player.UnitBeingMoved;
		var plrMover = mover.AsPlayer;

		if (plrMover && plrMover.IsBeingTeleported)
			return;

		Player.ValidateMovementInfo(movementInfo);

		if (movementInfo.Guid != mover.GUID)
		{
			Log.outError(LogFilter.Network, "HandleMovementOpcodes: guid error");

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
						Log.outDebug(LogFilter.Player, $"FALLDAMAGE Below map. Map min height: {plrMover.Map.GetMinHeight(plrMover.PhaseShift, movementInfo.Pos.X, movementInfo.Pos.Y)}, Player debug info:\n{plrMover.GetDebugInfo()}");
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
	void HandleMoveWorldportAck(WorldPortResponse packet)
	{
		HandleMoveWorldportAck();
	}

	void HandleMoveWorldportAck()
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
		if (!GridDefines.IsValidMapCoord(loc))
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
			Log.outError(LogFilter.Network, $"Player (Name {player.GetName()}) is still in world when teleported from map {oldMap.Id} to new map {loc.MapId}");
			oldMap.RemovePlayerFromMap(player, false);
		}

		// relocate the player to the teleport destination
		// the CannotEnter checks are done in TeleporTo but conditions may change
		// while the player is in transit, for example the map may get full
		if (newMap == null || newMap.CannotEnter(player) != null)
		{
			Log.outError(LogFilter.Network, $"Map {loc.MapId} could not be created for {(newMap ? newMap.MapName : "Unknown")} ({player.GUID}), porting player to homebind");
			player.TeleportTo(player.Homebind);

			return;
		}

		var z = loc.Z + player.HoverOffset;
		player.Location.Relocate(loc.X, loc.Y, z, loc.Orientation);
		player.SetFallInformation(0, player.Location.Z);

		player.ResetMap();
		player.Map = newMap;

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
			Log.outError(LogFilter.Network, $"WORLD: failed to teleport player {player.GetName()} ({player.GUID}) to map {loc.MapId} ({(newMap ? newMap.MapName : "Unknown")}) because of unknown reason!");
			player.ResetMap();
			player.Map = oldMap;
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

		if (!player.IsAlive && player.TeleportOptions.HasAnyFlag(TeleportToOptions.ReviveAtTeleport))
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

	[WorldPacketHandler(ClientOpcodes.SummonResponse)]
	void HandleSummonResponseOpcode(SummonResponse packet)
	{
		if (!Player.IsAlive || Player.IsInCombat)
			return;

		Player.SummonIfPossible(packet.Accept);
	}

	[WorldPacketHandler(ClientOpcodes.MoveRemoveMovementForceAck, Processing = PacketProcessing.ThreadSafe)]
	void HandleMoveRemoveMovementForceAck(MoveRemoveMovementForceAck moveRemoveMovementForceAck)
	{
		var mover = _player.UnitBeingMoved;
		_player.ValidateMovementInfo(moveRemoveMovementForceAck.Ack.Status);

		// prevent tampered movement data
		if (moveRemoveMovementForceAck.Ack.Status.Guid != mover.GUID)
		{
			Log.outError(LogFilter.Network, $"HandleMoveRemoveMovementForceAck: guid error, expected {mover.GUID}, got {moveRemoveMovementForceAck.Ack.Status.Guid}");

			return;
		}

		moveRemoveMovementForceAck.Ack.Status.Time = AdjustClientMovementTime(moveRemoveMovementForceAck.Ack.Status.Time);

		MoveUpdateRemoveMovementForce updateRemoveMovementForce = new();
		updateRemoveMovementForce.Status = moveRemoveMovementForceAck.Ack.Status;
		updateRemoveMovementForce.TriggerGUID = moveRemoveMovementForceAck.ID;
		mover.SendMessageToSet(updateRemoveMovementForce, false);
	}

	[WorldPacketHandler(ClientOpcodes.TimeSyncResponse, Processing = PacketProcessing.ThreadSafe)]
	void HandleTimeSyncResponse(TimeSyncResponse timeSyncResponse)
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

	void ComputeNewClockDelta()
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
}