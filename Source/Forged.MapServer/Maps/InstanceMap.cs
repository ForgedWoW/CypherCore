// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Framework.Database;
using Game.Entities;
using Game.Scenarios;
using Game.Scripting.Interfaces.IMap;
using Game.Common.Groups;
using Game.Common.Networking.Packets.Instance;
using Game.Common.Server;

namespace Game.Maps;

public class InstanceMap : Map
{
	readonly InstanceLock _instanceLock;
	readonly GroupInstanceReference _owningGroupRef = new();
	InstanceScript _data;
	uint _scriptId;
	InstanceScenario _scenario;
	DateTime? _instanceExpireEvent;

	public uint MaxPlayers
	{
		get
		{
			var mapDiff = MapDifficulty;

			if (mapDiff != null && mapDiff.MaxPlayers != 0)
				return mapDiff.MaxPlayers;

			return Entry.MaxPlayers;
		}
	}

	public int TeamIdInInstance
	{
		get
		{
			if (Global.WorldStateMgr.GetValue(WorldStates.TeamInInstanceAlliance, this) != 0)
				return TeamIds.Alliance;

			if (Global.WorldStateMgr.GetValue(WorldStates.TeamInInstanceHorde, this) != 0)
				return TeamIds.Horde;

			return TeamIds.Neutral;
		}
	}

	public TeamFaction TeamInInstance => TeamIdInInstance == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde;

	public uint ScriptId => _scriptId;

	public InstanceScript InstanceScript => _data;

	public InstanceScenario InstanceScenario => _scenario;

	public InstanceLock InstanceLock => _instanceLock;

	public InstanceMap(uint id, long expiry, uint InstanceId, Difficulty spawnMode, int instanceTeam, InstanceLock instanceLock) : base(id, expiry, InstanceId, spawnMode)
	{
		_instanceLock = instanceLock;

		//lets initialize visibility distance for dungeons
		InitVisibilityDistance();

		// the timer is started by default, and stopped when the first player joins
		// this make sure it gets unloaded if for some reason no player joins
		UnloadTimer = (uint)Math.Max(WorldConfig.GetIntValue(WorldCfg.InstanceUnloadDelay), 1);

		Global.WorldStateMgr.SetValue(WorldStates.TeamInInstanceAlliance, instanceTeam == TeamIds.Alliance ? 1 : 0, false, this);
		Global.WorldStateMgr.SetValue(WorldStates.TeamInInstanceHorde, instanceTeam == TeamIds.Horde ? 1 : 0, false, this);

		if (_instanceLock != null)
		{
			_instanceLock.SetInUse(true);
			_instanceExpireEvent = _instanceLock.GetExpiryTime(); // ignore extension state for reset event (will ask players to accept extended save on expiration)
		}
	}

	public override void InitVisibilityDistance()
	{
		//init visibility distance for instances
		VisibleDistance = Global.WorldMgr.MaxVisibleDistanceInInstances;
		VisibilityNotifyPeriod = Global.WorldMgr.VisibilityNotifyPeriodInInstances;
	}

	public override TransferAbortParams CannotEnter(Player player)
	{
		if (player.Map == this)
		{
			Log.outError(LogFilter.Maps, "InstanceMap:CannotEnter - player {0} ({1}) already in map {2}, {3}, {4}!", player.GetName(), player.GUID.ToString(), Id, InstanceId, DifficultyID);
			return new TransferAbortParams(TransferAbortReason.Error);
		}

		// allow GM's to enter
		if (player.IsGameMaster)
			return base.CannotEnter(player);

		// cannot enter if the instance is full (player cap), GMs don't count
		var maxPlayers = MaxPlayers;

		if (GetPlayersCountExceptGMs() >= maxPlayers)
		{
			Log.outInfo(LogFilter.Maps, "MAP: Instance '{0}' of map '{1}' cannot have more than '{2}' players. Player '{3}' rejected", InstanceId, MapName, maxPlayers, player.GetName());

			return new TransferAbortParams(TransferAbortReason.MaxPlayers);
		}

		// cannot enter while an encounter is in progress (unless this is a relog, in which case it is permitted)
		if (!player.IsLoading && IsRaid && InstanceScript != null && InstanceScript.IsEncounterInProgress())
			return new TransferAbortParams(TransferAbortReason.ZoneInCombat);

		if (_instanceLock != null)
		{
			// cannot enter if player is permanent saved to a different instance id
			var lockError = Global.InstanceLockMgr.CanJoinInstanceLock(player.GUID, new MapDb2Entries(Entry, MapDifficulty), _instanceLock);

			if (lockError != TransferAbortReason.None)
				return new TransferAbortParams(lockError);
		}

		return base.CannotEnter(player);
	}

	public override bool AddPlayerToMap(Player player, bool initPlayer = true)
	{
		// increase current instances (hourly limit)
		player.AddInstanceEnterTime(InstanceId, GameTime.GetGameTime());

		MapDb2Entries entries = new(Entry, MapDifficulty);

		if (entries.MapDifficulty.HasResetSchedule() && _instanceLock != null && _instanceLock.GetData().CompletedEncountersMask != 0)
			if (!entries.MapDifficulty.IsUsingEncounterLocks())
			{
				var playerLock = Global.InstanceLockMgr.FindActiveInstanceLock(player.GUID, entries);

				if (playerLock == null ||
					(playerLock.IsExpired() && playerLock.IsExtended()) ||
					playerLock.GetData().CompletedEncountersMask != _instanceLock.GetData().CompletedEncountersMask)
				{
					PendingRaidLock pendingRaidLock = new();
					pendingRaidLock.TimeUntilLock = 60000;
					pendingRaidLock.CompletedMask = _instanceLock.GetData().CompletedEncountersMask;
					pendingRaidLock.Extending = playerLock != null && playerLock.IsExtended();
					pendingRaidLock.WarningOnly = entries.Map.IsFlexLocking(); // events it triggers:  1 : INSTANCE_LOCK_WARNING   0 : INSTANCE_LOCK_STOP / INSTANCE_LOCK_START
					player.Session.SendPacket(pendingRaidLock);

					if (!entries.Map.IsFlexLocking())
						player.SetPendingBind(InstanceId, 60000);
				}
			}

		Log.outInfo(LogFilter.Maps,
					"MAP: Player '{0}' entered instance '{1}' of map '{2}'",
					player.GetName(),
					InstanceId,
					MapName);

		// initialize unload state
		UnloadTimer = 0;

		// this will acquire the same mutex so it cannot be in the previous block
		base.AddPlayerToMap(player, initPlayer);

		if (_data != null)
			_data.OnPlayerEnter(player);

		if (_scenario != null)
			_scenario.OnPlayerEnter(player);

		return true;
	}

	public override void Update(uint diff)
	{
		base.Update(diff);

		if (_data != null)
		{
			_data.Update(diff);
			_data.UpdateCombatResurrection(diff);
		}

		if (_scenario != null)
			_scenario.Update(diff);

		if (_instanceExpireEvent.HasValue && _instanceExpireEvent.Value < GameTime.GetSystemTime())
		{
			Reset(InstanceResetMethod.Expire);
			_instanceExpireEvent = Global.InstanceLockMgr.GetNextResetTime(new MapDb2Entries(Entry, MapDifficulty));
		}
	}

	public override void RemovePlayerFromMap(Player player, bool remove)
	{
		Log.outInfo(LogFilter.Maps, "MAP: Removing player '{0}' from instance '{1}' of map '{2}' before relocating to another map", player.GetName(), InstanceId, MapName);

		if (_data != null)
			_data.OnPlayerLeave(player);

		// if last player set unload timer
		if (UnloadTimer == 0 && Players.Count == 1)
			UnloadTimer = (_instanceLock != null && _instanceLock.IsExpired()) ? 1 : (uint)Math.Max(WorldConfig.GetIntValue(WorldCfg.InstanceUnloadDelay), 1);

		if (_scenario != null)
			_scenario.OnPlayerExit(player);

		base.RemovePlayerFromMap(player, remove);
	}

	public void CreateInstanceData()
	{
		if (_data != null)
			return;

		var mInstance = Global.ObjectMgr.GetInstanceTemplate(Id);

		if (mInstance != null)
		{
			_scriptId = mInstance.ScriptId;
			_data = Global.ScriptMgr.RunScriptRet<IInstanceMapGetInstanceScript, InstanceScript>(p => p.GetInstanceScript(this), ScriptId, null);
		}

		if (_data == null)
			return;

		if (_instanceLock == null || _instanceLock.GetInstanceId() == 0)
		{
			_data.Create();

			return;
		}

		MapDb2Entries entries = new(Entry, MapDifficulty);

		if (!entries.IsInstanceIdBound() || !IsRaid || !entries.MapDifficulty.IsRestoringDungeonState() || _owningGroupRef.IsValid())
		{
			_data.Create();

			return;
		}

		var lockData = _instanceLock.GetInstanceInitializationData();
		_data.SetCompletedEncountersMask(lockData.CompletedEncountersMask);
		_data.SetEntranceLocation(lockData.EntranceWorldSafeLocId);

		if (!lockData.Data.IsEmpty())
		{
			Log.outDebug(LogFilter.Maps, $"Loading instance data for `{Global.ObjectMgr.GetScriptName(_scriptId)}` with id {InstanceIdInternal}");
			_data.Load(lockData.Data);
		}
		else
		{
			_data.Create();
		}
	}

	public PlayerGroup GetOwningGroup()
	{
		return _owningGroupRef.Target;
	}

	public void TrySetOwningGroup(PlayerGroup group)
	{
		if (!_owningGroupRef.IsValid())
			_owningGroupRef.Link(group, this);
	}

	public InstanceResetResult Reset(InstanceResetMethod method)
	{
		// raids can be reset if no boss was killed
		if (method != InstanceResetMethod.Expire && _instanceLock != null && _instanceLock.GetData().CompletedEncountersMask != 0)
			return InstanceResetResult.CannotReset;

		if (HavePlayers)
		{
			switch (method)
			{
				case InstanceResetMethod.Manual:
					// notify the players to leave the instance so it can be reset
					foreach (var player in Players)
						player.SendResetFailedNotify(Id);

					break;
				case InstanceResetMethod.OnChangeDifficulty:
					// no client notification
					break;
				case InstanceResetMethod.Expire:
				{
					RaidInstanceMessage raidInstanceMessage = new();
					raidInstanceMessage.Type = InstanceResetWarningType.Expired;
					raidInstanceMessage.MapID = Id;
					raidInstanceMessage.DifficultyID = DifficultyID;
					raidInstanceMessage.Write();

					PendingRaidLock pendingRaidLock = new();
					pendingRaidLock.TimeUntilLock = 60000;
					pendingRaidLock.CompletedMask = _instanceLock.GetData().CompletedEncountersMask;
					pendingRaidLock.Extending = true;
					pendingRaidLock.WarningOnly = Entry.IsFlexLocking();
					pendingRaidLock.Write();

					foreach (var player in Players)
					{
						player.SendPacket(raidInstanceMessage);
						player.SendPacket(pendingRaidLock);

						if (!pendingRaidLock.WarningOnly)
							player.SetPendingBind(InstanceId, 60000);
					}

					break;
				}
				default:
					break;
			}

			return InstanceResetResult.NotEmpty;
		}
		else
		{
			// unloaded at next update
			UnloadTimer = 1;
		}

		return InstanceResetResult.Success;
	}

	public string GetScriptName()
	{
		return Global.ObjectMgr.GetScriptName(_scriptId);
	}

	public void UpdateInstanceLock(UpdateBossStateSaveDataEvent updateSaveDataEvent)
	{
		if (_instanceLock != null)
		{
			var instanceCompletedEncounters = _instanceLock.GetData().CompletedEncountersMask | (1u << updateSaveDataEvent.DungeonEncounter.Bit);

			MapDb2Entries entries = new(Entry, MapDifficulty);

			SQLTransaction trans = new();

			if (entries.IsInstanceIdBound())
				Global.InstanceLockMgr.UpdateSharedInstanceLock(trans,
																new InstanceLockUpdateEvent(InstanceId,
																							_data.GetSaveData(),
																							instanceCompletedEncounters,
																							updateSaveDataEvent.DungeonEncounter,
																							_data.GetEntranceLocationForCompletedEncounters(instanceCompletedEncounters)));

			foreach (var player in Players)
			{
				// never instance bind GMs with GM mode enabled
				if (player.IsGameMaster)
					continue;

				var playerLock = Global.InstanceLockMgr.FindActiveInstanceLock(player.GUID, entries);
				var oldData = "";
				uint playerCompletedEncounters = 0;

				if (playerLock != null)
				{
					oldData = playerLock.GetData().Data;
					playerCompletedEncounters = playerLock.GetData().CompletedEncountersMask | (1u << updateSaveDataEvent.DungeonEncounter.Bit);
				}

				var isNewLock = playerLock == null || playerLock.GetData().CompletedEncountersMask == 0 || playerLock.IsExpired();

				var newLock = Global.InstanceLockMgr.UpdateInstanceLockForPlayer(trans,
																				player.GUID,
																				entries,
																				new InstanceLockUpdateEvent(InstanceId,
																											_data.UpdateBossStateSaveData(oldData, updateSaveDataEvent),
																											instanceCompletedEncounters,
																											updateSaveDataEvent.DungeonEncounter,
																											_data.GetEntranceLocationForCompletedEncounters(playerCompletedEncounters)));

				if (isNewLock)
				{
					InstanceSaveCreated data = new();
					data.Gm = player.IsGameMaster;
					player.SendPacket(data);

					player.Session.SendCalendarRaidLockoutAdded(newLock);
				}
			}

			DB.Characters.CommitTransaction(trans);
		}
	}

	public void UpdateInstanceLock(UpdateAdditionalSaveDataEvent updateSaveDataEvent)
	{
		if (_instanceLock != null)
		{
			var instanceCompletedEncounters = _instanceLock.GetData().CompletedEncountersMask;

			MapDb2Entries entries = new(Entry, MapDifficulty);

			SQLTransaction trans = new();

			if (entries.IsInstanceIdBound())
				Global.InstanceLockMgr.UpdateSharedInstanceLock(trans, new InstanceLockUpdateEvent(InstanceId, _data.GetSaveData(), instanceCompletedEncounters, null, null));

			foreach (var player in Players)
			{
				// never instance bind GMs with GM mode enabled
				if (player.IsGameMaster)
					continue;

				var playerLock = Global.InstanceLockMgr.FindActiveInstanceLock(player.GUID, entries);
				var oldData = "";

				if (playerLock != null)
					oldData = playerLock.GetData().Data;

				var isNewLock = playerLock == null || playerLock.GetData().CompletedEncountersMask == 0 || playerLock.IsExpired();

				var newLock = Global.InstanceLockMgr.UpdateInstanceLockForPlayer(trans,
																				player.GUID,
																				entries,
																				new InstanceLockUpdateEvent(InstanceId,
																											_data.UpdateAdditionalSaveData(oldData, updateSaveDataEvent),
																											instanceCompletedEncounters,
																											null,
																											null));

				if (isNewLock)
				{
					InstanceSaveCreated data = new();
					data.Gm = player.IsGameMaster;
					player.SendPacket(data);

					player.Session.SendCalendarRaidLockoutAdded(newLock);
				}
			}

			DB.Characters.CommitTransaction(trans);
		}
	}

	public void CreateInstanceLockForPlayer(Player player)
	{
		MapDb2Entries entries = new(Entry, MapDifficulty);
		var playerLock = Global.InstanceLockMgr.FindActiveInstanceLock(player.GUID, entries);

		var isNewLock = playerLock == null || playerLock.GetData().CompletedEncountersMask == 0 || playerLock.IsExpired();

		SQLTransaction trans = new();

		var newLock = Global.InstanceLockMgr.UpdateInstanceLockForPlayer(trans, player.GUID, entries, new InstanceLockUpdateEvent(InstanceId, _data.GetSaveData(), _instanceLock.GetData().CompletedEncountersMask, null, null));

		DB.Characters.CommitTransaction(trans);

		if (isNewLock)
		{
			InstanceSaveCreated data = new();
			data.Gm = player.IsGameMaster;
			player.SendPacket(data);

			player.Session.SendCalendarRaidLockoutAdded(newLock);
		}
	}

	public override string GetDebugInfo()
	{
		return $"{base.GetDebugInfo()}\nScriptId: {ScriptId} ScriptName: {GetScriptName()}";
	}

	public void SetInstanceScenario(InstanceScenario scenario)
	{
		_scenario = scenario;
	}

	~InstanceMap()
	{
		if (_instanceLock != null)
			_instanceLock.SetInUse(false);
	}
}