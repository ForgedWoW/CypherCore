﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;
using Game.Spells;

namespace Game.Maps;

public class InstanceScript : ZoneScript
{
	readonly Dictionary<uint, BossInfo> _bosses = new();
	readonly List<PersistentInstanceScriptValueBase> _persistentScriptValues = new();
	readonly MultiMap<uint, DoorInfo> _doors = new();
	readonly Dictionary<uint, MinionInfo> _minions = new();
	readonly Dictionary<uint, uint> _creatureInfo = new();
	readonly Dictionary<uint, uint> _gameObjectInfo = new();
	readonly Dictionary<uint, ObjectGuid> _objectGuids = new();
	readonly List<InstanceSpawnGroupInfo> _instanceSpawnGroups = new();
	readonly List<uint> _activatedAreaTriggers = new();
	string _headers;
	uint _completedEncounters; // DEPRECATED, REMOVE
	uint _entranceId;
	uint _temporaryEntranceId;
	uint _combatResurrectionTimer;
	byte _combatResurrectionCharges; // the counter for available battle resurrections
	bool _combatResurrectionTimerStarted;

	public InstanceMap Instance { get; set; }

	public InstanceScript(InstanceMap map)
	{
		Instance = map;
		_instanceSpawnGroups = Global.ObjectMgr.GetInstanceSpawnGroupsForMap(map.Id);
	}

	public virtual bool IsEncounterInProgress()
	{
		foreach (var boss in _bosses.Values)
			if (boss.State == EncounterState.InProgress)
				return true;

		return false;
	}

	public override void OnCreatureCreate(Creature creature)
	{
		AddObject(creature, true);
		AddMinion(creature, true);
	}

	public override void OnCreatureRemove(Creature creature)
	{
		AddObject(creature, false);
		AddMinion(creature, false);
	}

	public override void OnGameObjectCreate(GameObject go)
	{
		AddObject(go, true);
		AddDoor(go, true);
	}

	public override void OnGameObjectRemove(GameObject go)
	{
		AddObject(go, false);
		AddDoor(go, false);
	}

	public ObjectGuid GetObjectGuid(uint type)
	{
		return _objectGuids.LookupByKey(type);
	}

	public override ObjectGuid GetGuidData(uint type)
	{
		return GetObjectGuid(type);
	}

	public void SetHeaders(string dataHeaders)
	{
		_headers = dataHeaders;
	}

	public void LoadBossBoundaries(BossBoundaryEntry[] data)
	{
		foreach (var entry in data)
			if (entry.BossId < _bosses.Count)
				_bosses[entry.BossId].Boundary.Add(entry.Boundary);
	}

	public void LoadMinionData(params MinionData[] data)
	{
		foreach (var minion in data)
		{
			if (minion.Entry == 0)
				continue;

			if (minion.BossId < _bosses.Count)
				_minions.Add(minion.Entry, new MinionInfo(_bosses[minion.BossId]));
		}

		Log.outDebug(LogFilter.Scripts, "InstanceScript.LoadMinionData: {0} minions loaded.", _minions.Count);
	}

	public void LoadDoorData(params DoorData[] data)
	{
		foreach (var door in data)
		{
			if (door.Entry == 0)
				continue;

			if (door.bossId < _bosses.Count)
				_doors.Add(door.Entry, new DoorInfo(_bosses[door.bossId], door.Type));
		}

		Log.outDebug(LogFilter.Scripts, "InstanceScript.LoadDoorData: {0} doors loaded.", _doors.Count);
	}

	public void LoadObjectData(ObjectData[] creatureData, ObjectData[] gameObjectData)
	{
		if (creatureData != null)
			LoadObjectData(creatureData, _creatureInfo);

		if (gameObjectData != null)
			LoadObjectData(gameObjectData, _gameObjectInfo);

		Log.outDebug(LogFilter.Scripts, "InstanceScript.LoadObjectData: {0} objects loaded.", _creatureInfo.Count + _gameObjectInfo.Count);
	}

	public void LoadDungeonEncounterData(DungeonEncounterData[] encounters)
	{
		foreach (var encounter in encounters)
			LoadDungeonEncounterData(encounter.BossId, encounter.DungeonEncounterId);
	}

	public virtual void UpdateDoorState(GameObject door)
	{
		var range = _doors.LookupByKey(door.Entry);

		if (range.Empty())
			return;

		var open = true;

		foreach (var info in range)
		{
			if (!open)
				break;

			switch (info.Type)
			{
				case DoorType.Room:
					open = (info.BossInfo.State != EncounterState.InProgress);

					break;
				case DoorType.Passage:
					open = (info.BossInfo.State == EncounterState.Done);

					break;
				case DoorType.SpawnHole:
					open = (info.BossInfo.State == EncounterState.InProgress);

					break;
				default:
					break;
			}
		}

		door.SetGoState(open ? GameObjectState.Active : GameObjectState.Ready);
	}

	public BossInfo GetBossInfo(uint id)
	{
		return _bosses[id];
	}

	public virtual void AddDoor(GameObject door, bool add)
	{
		var range = _doors.LookupByKey(door.Entry);

		if (range.Empty())
			return;

		foreach (var data in range)
			if (add)
				data.BossInfo.Door[(int)data.Type].Add(door.GUID);
			else
				data.BossInfo.Door[(int)data.Type].Remove(door.GUID);

		if (add)
			UpdateDoorState(door);
	}

	public void AddMinion(Creature minion, bool add)
	{
		var minionInfo = _minions.LookupByKey(minion.Entry);

		if (minionInfo == null)
			return;

		if (add)
			minionInfo.BossInfo.Minion.Add(minion.GUID);
		else
			minionInfo.BossInfo.Minion.Remove(minion.GUID);
	}

	// Triggers a GameEvent
	// * If source is null then event is triggered for each player in the instance as "source"
	public override void TriggerGameEvent(uint gameEventId, WorldObject source = null, WorldObject target = null)
	{
		if (source != null)
		{
			base.TriggerGameEvent(gameEventId, source, target);

			return;
		}

		ProcessEvent(target, gameEventId, source);
		Instance.DoOnPlayers(player => GameEvents.TriggerForPlayer(gameEventId, player));

		GameEvents.TriggerForMap(gameEventId, Instance);
	}

	public Creature GetCreature(uint type)
	{
		return Instance.GetCreature(GetObjectGuid(type));
	}

	public GameObject GetGameObject(uint type)
	{
		return Instance.GetGameObject(GetObjectGuid(type));
	}

	public virtual bool SetBossState(uint id, EncounterState state)
	{
		if (id < _bosses.Count)
		{
			var bossInfo = _bosses[id];

			if (bossInfo.State == EncounterState.ToBeDecided) // loading
			{
				bossInfo.State = state;
				Log.outDebug(LogFilter.Scripts, $"InstanceScript: Initialize boss {id} state as {state} (map {Instance.Id}, {Instance.InstanceId}).");

				return false;
			}
			else
			{
				if (bossInfo.State == state)
					return false;

				if (bossInfo.State == EncounterState.Done)
				{
					Log.outError(LogFilter.Maps, $"InstanceScript: Tried to set instance boss {id} state from {bossInfo.State} back to {state} for map {Instance.Id}, instance id {Instance.InstanceId}. Blocked!");

					return false;
				}

				if (state == EncounterState.Done)
					foreach (var guid in bossInfo.Minion)
					{
						var minion = Instance.GetCreature(guid);

						if (minion)
							if (minion.IsWorldBoss && minion.IsAlive)
								return false;
					}

				DungeonEncounterRecord dungeonEncounter = null;

				switch (state)
				{
					case EncounterState.InProgress:
					{
						var resInterval = GetCombatResurrectionChargeInterval();
						InitializeCombatResurrections(1, resInterval);
						SendEncounterStart(1, 9, resInterval, resInterval);

						Instance.DoOnPlayers(player =>
						{
							if (player.IsAlive)
								Unit.ProcSkillsAndAuras(player, null, new ProcFlagsInit(ProcFlags.EncounterStart), new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
						});

						break;
					}
					case EncounterState.Fail:
						ResetCombatResurrections();
						SendEncounterEnd();

						break;
					case EncounterState.Done:
						ResetCombatResurrections();
						SendEncounterEnd();
						dungeonEncounter = bossInfo.GetDungeonEncounterForDifficulty(Instance.DifficultyID);

						if (dungeonEncounter != null)
						{
							DoUpdateCriteria(CriteriaType.DefeatDungeonEncounter, dungeonEncounter.Id);
							SendBossKillCredit(dungeonEncounter.Id);
						}

						break;
					default:
						break;
				}

				bossInfo.State = state;

				if (dungeonEncounter != null)
					Instance.UpdateInstanceLock(new UpdateBossStateSaveDataEvent(dungeonEncounter, id, state));
			}

			for (uint type = 0; type < (int)DoorType.Max; ++type)
				foreach (var guid in bossInfo.Door[type])
				{
					var door = Instance.GetGameObject(guid);

					if (door)
						UpdateDoorState(door);
				}

			foreach (var guid in bossInfo.Minion.ToList())
			{
				var minion = Instance.GetCreature(guid);

				if (minion)
					UpdateMinionState(minion, state);
			}

			UpdateSpawnGroups();

			return true;
		}

		return false;
	}

	public bool _SkipCheckRequiredBosses(Player player = null)
	{
		return player && player.Session.HasPermission(RBACPermissions.SkipCheckInstanceRequiredBosses);
	}

	public virtual void Create()
	{
		for (uint i = 0; i < _bosses.Count; ++i)
			SetBossState(i, EncounterState.NotStarted);

		UpdateSpawnGroups();
	}

	public void Load(string data)
	{
		if (string.IsNullOrEmpty(data))
		{
			OutLoadInstDataFail();

			return;
		}

		OutLoadInstData(data);

		InstanceScriptDataReader reader = new(this);

		if (reader.Load(data) == InstanceScriptDataReader.Result.Ok)
		{
			// in loot-based lockouts instance can be loaded with later boss marked as killed without preceding bosses
			// but we still need to have them alive
			for (uint i = 0; i < _bosses.Count; ++i)
				if (_bosses[i].State == EncounterState.Done && !CheckRequiredBosses(i))
					_bosses[i].State = EncounterState.NotStarted;

			UpdateSpawnGroups();
			AfterDataLoad();
		}
		else
		{
			OutLoadInstDataFail();
		}

		OutLoadInstDataComplete();
	}

	public string GetSaveData()
	{
		OutSaveInstData();

		InstanceScriptDataWriter writer = new(this);

		writer.FillData();

		OutSaveInstDataComplete();

		return writer.GetString();
	}

	public string UpdateBossStateSaveData(string oldData, UpdateBossStateSaveDataEvent saveEvent)
	{
		if (!Instance.MapDifficulty.IsUsingEncounterLocks())
			return GetSaveData();

		InstanceScriptDataWriter writer = new(this);
		writer.FillDataFrom(oldData);
		writer.SetBossState(saveEvent);

		return writer.GetString();
	}

	public string UpdateAdditionalSaveData(string oldData, UpdateAdditionalSaveDataEvent saveEvent)
	{
		if (!Instance.MapDifficulty.IsUsingEncounterLocks())
			return GetSaveData();

		InstanceScriptDataWriter writer = new(this);
		writer.FillDataFrom(oldData);
		writer.SetAdditionalData(saveEvent);

		return writer.GetString();
	}

	public uint? GetEntranceLocationForCompletedEncounters(uint completedEncountersMask)
	{
		if (!Instance.MapDifficulty.IsUsingEncounterLocks())
			return _entranceId;

		return ComputeEntranceLocationForCompletedEncounters(completedEncountersMask);
	}

	public virtual uint? ComputeEntranceLocationForCompletedEncounters(uint completedEncountersMask)
	{
		return null;
	}

	public void HandleGameObject(ObjectGuid guid, bool open, GameObject go = null)
	{
		if (!go)
			go = Instance.GetGameObject(guid);

		if (go)
			go.SetGoState(open ? GameObjectState.Active : GameObjectState.Ready);
		else
			Log.outDebug(LogFilter.Scripts, "InstanceScript: HandleGameObject failed");
	}

	public void DoUseDoorOrButton(ObjectGuid uiGuid, uint withRestoreTime = 0, bool useAlternativeState = false)
	{
		if (uiGuid.IsEmpty)
			return;

		var go = Instance.GetGameObject(uiGuid);

		if (go)
		{
			if (go.GoType == GameObjectTypes.Door || go.GoType == GameObjectTypes.Button)
			{
				if (go.LootState == LootState.Ready)
					go.UseDoorOrButton(withRestoreTime, useAlternativeState);
				else if (go.LootState == LootState.Activated)
					go.ResetDoorOrButton();
			}
			else
			{
				Log.outError(LogFilter.Scripts, "InstanceScript: DoUseDoorOrButton can't use gameobject entry {0}, because type is {1}.", go.Entry, go.GoType);
			}
		}
		else
		{
			Log.outDebug(LogFilter.Scripts, "InstanceScript: DoUseDoorOrButton failed");
		}
	}

	public void DoRespawnGameObject(ObjectGuid guid, TimeSpan timeToDespawn)
	{
		var go = Instance.GetGameObject(guid);

		if (go)
		{
			switch (go.GoType)
			{
				case GameObjectTypes.Door:
				case GameObjectTypes.Button:
				case GameObjectTypes.Trap:
				case GameObjectTypes.FishingNode:
					// not expect any of these should ever be handled
					Log.outError(LogFilter.Scripts, "InstanceScript: DoRespawnGameObject can't respawn gameobject entry {0}, because type is {1}.", go.Entry, go.GoType);

					return;
				default:
					break;
			}

			if (go.IsSpawned)
				return;

			go.SetRespawnTime((int)timeToDespawn.TotalSeconds);
		}
		else
		{
			Log.outDebug(LogFilter.Scripts, "InstanceScript: DoRespawnGameObject failed");
		}
	}

	public void DoUpdateWorldState(uint worldStateId, int value)
	{
		Global.WorldStateMgr.SetValue(worldStateId, value, false, Instance);
	}

	// Update Achievement Criteria for all players in instance
	public void DoUpdateCriteria(CriteriaType type, uint miscValue1 = 0, uint miscValue2 = 0, Unit unit = null)
	{
		Instance.DoOnPlayers(player => player.UpdateCriteria(type, miscValue1, miscValue2, 0, unit));
	}

	// Remove Auras due to Spell on all players in instance
	public void DoRemoveAurasDueToSpellOnPlayers(uint spell, bool includePets = false, bool includeControlled = false)
	{
		Instance.DoOnPlayers(player => DoRemoveAurasDueToSpellOnPlayer(player, spell, includePets, includeControlled));
	}

	public void DoRemoveAurasDueToSpellOnPlayer(Player player, uint spell, bool includePets = false, bool includeControlled = false)
	{
		if (!player)
			return;

		player.RemoveAura(spell);

		if (!includePets)
			return;

		for (var i = 0; i < SharedConst.MaxSummonSlot; ++i)
		{
			var summonGUID = player.SummonSlot[i];

			if (!summonGUID.IsEmpty)
			{
				var summon = Instance.GetCreature(summonGUID);

				if (summon != null)
					summon.RemoveAura(spell);
			}
		}

		if (!includeControlled)
			return;

		for (var i = 0; i < player.Controlled.Count; ++i)
		{
			var controlled = player.Controlled[i];

			if (controlled != null)
				if (controlled.IsInWorld && controlled.IsCreature)
					controlled.RemoveAura(spell);
		}
	}

	// Cast spell on all players in instance
	public void DoCastSpellOnPlayers(uint spell, bool includePets = false, bool includeControlled = false)
	{
		Instance.DoOnPlayers(player => DoCastSpellOnPlayer(player, spell, includePets, includeControlled));
	}

	public void DoCastSpellOnPlayer(Player player, uint spell, bool includePets = false, bool includeControlled = false)
	{
		if (!player)
			return;

		player.CastSpell(player, spell, true);

		if (!includePets)
			return;

		for (var i = 0; i < SharedConst.MaxSummonSlot; ++i)
		{
			var summonGUID = player.SummonSlot[i];

			if (!summonGUID.IsEmpty)
			{
				var summon = Instance.GetCreature(summonGUID);

				if (summon != null)
					summon.CastSpell(player, spell, true);
			}
		}

		if (!includeControlled)
			return;

		for (var i = 0; i < player.Controlled.Count; ++i)
		{
			var controlled = player.Controlled[i];

			if (controlled != null)
				if (controlled.IsInWorld && controlled.IsCreature)
					controlled.CastSpell(player, spell, true);
		}
	}

	public DungeonEncounterRecord GetBossDungeonEncounter(uint id)
	{
		return id < _bosses.Count ? _bosses[id].GetDungeonEncounterForDifficulty(Instance.DifficultyID) : null;
	}

	public DungeonEncounterRecord GetBossDungeonEncounter(Creature creature)
	{
		var bossAi = creature.AI as BossAI;

		if (bossAi != null)
			return GetBossDungeonEncounter(bossAi.GetBossId());

		return null;
	}

	public virtual bool CheckAchievementCriteriaMeet(uint criteria_id, Player source, Unit target = null, uint miscvalue1 = 0)
	{
		Log.outError(LogFilter.Server,
					"Achievement system call CheckAchievementCriteriaMeet but instance script for map {0} not have implementation for achievement criteria {1}",
					Instance.Id,
					criteria_id);

		return false;
	}

	public bool IsEncounterCompleted(uint dungeonEncounterId)
	{
		for (uint i = 0; i < _bosses.Count; ++i)
			for (var j = 0; j < _bosses[i].DungeonEncounters.Length; ++j)
				if (_bosses[i].DungeonEncounters[j] != null && _bosses[i].DungeonEncounters[j].Id == dungeonEncounterId)
					return _bosses[i].State == EncounterState.Done;

		return false;
	}

	public bool IsEncounterCompletedInMaskByBossId(uint completedEncountersMask, uint bossId)
	{
		var dungeonEncounter = GetBossDungeonEncounter(bossId);

		if (dungeonEncounter != null)
			if ((completedEncountersMask & (1 << dungeonEncounter.Bit)) != 0)
				return _bosses[bossId].State == EncounterState.Done;

		return false;
	}

	public void SetEntranceLocation(uint worldSafeLocationId)
	{
		_entranceId = worldSafeLocationId;
		_temporaryEntranceId = 0;
	}

	public void SendEncounterUnit(EncounterFrameType type, Unit unit = null, byte priority = 0)
	{
		switch (type)
		{
			case EncounterFrameType.Engage:
				if (unit == null)
					return;

				InstanceEncounterEngageUnit encounterEngageMessage = new();
				encounterEngageMessage.Unit = unit.GUID;
				encounterEngageMessage.TargetFramePriority = priority;
				Instance.SendToPlayers(encounterEngageMessage);

				break;
			case EncounterFrameType.Disengage:
				if (!unit)
					return;

				InstanceEncounterDisengageUnit encounterDisengageMessage = new();
				encounterDisengageMessage.Unit = unit.GUID;
				Instance.SendToPlayers(encounterDisengageMessage);

				break;
			case EncounterFrameType.UpdatePriority:
				if (!unit)
					return;

				InstanceEncounterChangePriority encounterChangePriorityMessage = new();
				encounterChangePriorityMessage.Unit = unit.GUID;
				encounterChangePriorityMessage.TargetFramePriority = priority;
				Instance.SendToPlayers(encounterChangePriorityMessage);

				break;
			default:
				break;
		}
	}

	public void SendBossKillCredit(uint encounterId)
	{
		BossKill bossKillCreditMessage = new();
		bossKillCreditMessage.DungeonEncounterID = encounterId;

		Instance.SendToPlayers(bossKillCreditMessage);
	}

	public void UpdateEncounterStateForKilledCreature(uint creatureId, Unit source)
	{
		UpdateEncounterState(EncounterCreditType.KillCreature, creatureId, source);
	}

	public void UpdateEncounterStateForSpellCast(uint spellId, Unit source)
	{
		UpdateEncounterState(EncounterCreditType.CastSpell, spellId, source);
	}

	public void SetCompletedEncountersMask(uint newMask)
	{
		_completedEncounters = newMask;

		var encounters = Global.ObjectMgr.GetDungeonEncounterList(Instance.Id, Instance.DifficultyID);

		if (encounters != null)
			foreach (var encounter in encounters)
				if ((_completedEncounters & (1 << encounter.dbcEntry.Bit)) != 0 && encounter.dbcEntry.CompleteWorldStateID != 0)
					DoUpdateWorldState((uint)encounter.dbcEntry.CompleteWorldStateID, 1);
	}

	public void UpdateCombatResurrection(uint diff)
	{
		if (!_combatResurrectionTimerStarted)
			return;

		if (_combatResurrectionTimer <= diff)
			AddCombatResurrectionCharge();
		else
			_combatResurrectionTimer -= diff;
	}

	public void AddCombatResurrectionCharge()
	{
		++_combatResurrectionCharges;
		_combatResurrectionTimer = GetCombatResurrectionChargeInterval();
		_combatResurrectionTimerStarted = true;

		var gainCombatResurrectionCharge = new InstanceEncounterGainCombatResurrectionCharge();
		gainCombatResurrectionCharge.InCombatResCount = _combatResurrectionCharges;
		gainCombatResurrectionCharge.CombatResChargeRecovery = _combatResurrectionTimer;
		Instance.SendToPlayers(gainCombatResurrectionCharge);
	}

	public void UseCombatResurrection()
	{
		--_combatResurrectionCharges;

		Instance.SendToPlayers(new InstanceEncounterInCombatResurrection());
	}

	public void ResetCombatResurrections()
	{
		_combatResurrectionCharges = 0;
		_combatResurrectionTimer = 0;
		_combatResurrectionTimerStarted = false;
	}

	public uint GetCombatResurrectionChargeInterval()
	{
		uint interval = 0;
		var playerCount = Instance.Players.Count;

		if (playerCount != 0)
			interval = (uint)(90 * Time.Minute * Time.InMilliseconds / playerCount);

		return interval;
	}

	public bool InstanceHasScript(WorldObject obj, string scriptName)
	{
		var instance = obj.Map.ToInstanceMap;

		if (instance != null)
			return instance.GetScriptName() == scriptName;

		return false;
	}

	public virtual void Update(uint diff) { }

	// Called when a player successfully enters the instance.
	public virtual void OnPlayerEnter(Player player) { }

	// Called when a player successfully leaves the instance.
	public virtual void OnPlayerLeave(Player player) { }

	// Return wether server allow two side groups or not
	public bool ServerAllowsTwoSideGroups()
	{
		return WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGroup);
	}

	public EncounterState GetBossState(uint id)
	{
		return id < _bosses.Count ? _bosses[id].State : EncounterState.ToBeDecided;
	}

	public List<AreaBoundary> GetBossBoundary(uint id)
	{
		return id < _bosses.Count ? _bosses[id].Boundary : null;
	}

	public virtual bool CheckRequiredBosses(uint bossId, Player player = null)
	{
		return true;
	}

	public uint GetCompletedEncounterMask()
	{
		return _completedEncounters;
	}

	// Sets a temporary entrance that does not get saved to db
	public void SetTemporaryEntranceLocation(uint worldSafeLocationId)
	{
		_temporaryEntranceId = worldSafeLocationId;
	}

	// Get's the current entrance id
	public uint GetEntranceLocation()
	{
		return _temporaryEntranceId != 0 ? _temporaryEntranceId : _entranceId;
	}

	// Only used by areatriggers that inherit from OnlyOnceAreaTriggerScript
	public void MarkAreaTriggerDone(uint id)
	{
		_activatedAreaTriggers.Add(id);
	}

	public void ResetAreaTriggerDone(uint id)
	{
		_activatedAreaTriggers.Remove(id);
	}

	public bool IsAreaTriggerDone(uint id)
	{
		return _activatedAreaTriggers.Contains(id);
	}

	public int GetEncounterCount()
	{
		return _bosses.Count;
	}

	public byte GetCombatResurrectionCharges()
	{
		return _combatResurrectionCharges;
	}

	public void RegisterPersistentScriptValue(PersistentInstanceScriptValueBase value)
	{
		_persistentScriptValues.Add(value);
	}

	public string GetHeader()
	{
		return _headers;
	}

	public List<PersistentInstanceScriptValueBase> GetPersistentScriptValues()
	{
		return _persistentScriptValues;
	}

	public void SetBossNumber(uint number)
	{
		for (uint i = 0; i < number; ++i)
			_bosses.Add(i, new BossInfo());
	}

	public void OutSaveInstData()
	{
		Log.outDebug(LogFilter.Scripts, "Saving Instance Data for Instance {0} (Map {1}, Instance Id {2})", Instance.MapName, Instance.Id, Instance.InstanceId);
	}

	public void OutSaveInstDataComplete()
	{
		Log.outDebug(LogFilter.Scripts, "Saving Instance Data for Instance {0} (Map {1}, Instance Id {2}) completed.", Instance.MapName, Instance.Id, Instance.InstanceId);
	}

	public void OutLoadInstData(string input)
	{
		Log.outDebug(LogFilter.Scripts, "Loading Instance Data for Instance {0} (Map {1}, Instance Id {2}). Input is '{3}'", Instance.MapName, Instance.Id, Instance.InstanceId, input);
	}

	public void OutLoadInstDataComplete()
	{
		Log.outDebug(LogFilter.Scripts, "Instance Data Load for Instance {0} (Map {1}, Instance Id: {2}) is complete.", Instance.MapName, Instance.Id, Instance.InstanceId);
	}

	public void OutLoadInstDataFail()
	{
		Log.outDebug(LogFilter.Scripts, "Unable to load Instance Data for Instance {0} (Map {1}, Instance Id: {2}).", Instance.MapName, Instance.Id, Instance.InstanceId);
	}

	public List<InstanceSpawnGroupInfo> GetInstanceSpawnGroups()
	{
		return _instanceSpawnGroups;
	}

	// Override this function to validate all additional data loads
	public virtual void AfterDataLoad() { }

	void LoadObjectData(ObjectData[] objectData, Dictionary<uint, uint> objectInfo)
	{
		foreach (var data in objectData)
		{
			objectInfo[data.Entry] = data.Type;
		}
	}

	void LoadDungeonEncounterData(uint bossId, uint[] dungeonEncounterIds)
	{
		if (bossId < _bosses.Count)
			for (var i = 0; i < dungeonEncounterIds.Length && i < MapConst.MaxDungeonEncountersPerBoss; ++i)
				_bosses[bossId].DungeonEncounters[i] = CliDB.DungeonEncounterStorage.LookupByKey(dungeonEncounterIds[i]);
	}

	void UpdateMinionState(Creature minion, EncounterState state)
	{
		switch (state)
		{
			case EncounterState.NotStarted:
				if (!minion.IsAlive)
					minion.Respawn();
				else if (minion.IsInCombat)
					minion.AI.EnterEvadeMode();

				break;
			case EncounterState.InProgress:
				if (!minion.IsAlive)
					minion.Respawn();
				else if (minion.Victim == null)
					minion.AI.DoZoneInCombat();

				break;
			default:
				break;
		}
	}

	void UpdateSpawnGroups()
	{
		if (_instanceSpawnGroups.Empty())
			return;

		Dictionary<uint, InstanceState> newStates = new();

		foreach (var info in _instanceSpawnGroups)
		{
			if (!newStates.ContainsKey(info.SpawnGroupId))
				newStates[info.SpawnGroupId] = 0; // makes sure there's a BLOCK value in the map

			if (newStates[info.SpawnGroupId] == InstanceState.ForceBlock) // nothing will change this
				continue;

			if (((1 << (int)GetBossState(info.BossStateId)) & info.BossStates) == 0)
				continue;

			if (((Instance.TeamIdInInstance == TeamIds.Alliance) && info.Flags.HasFlag(InstanceSpawnGroupFlags.HordeOnly)) || ((Instance.TeamIdInInstance == TeamIds.Horde) && info.Flags.HasFlag(InstanceSpawnGroupFlags.AllianceOnly)))
				continue;

			if (info.Flags.HasAnyFlag(InstanceSpawnGroupFlags.BlockSpawn))
				newStates[info.SpawnGroupId] = InstanceState.ForceBlock;

			else if (info.Flags.HasAnyFlag(InstanceSpawnGroupFlags.ActivateSpawn))
				newStates[info.SpawnGroupId] = InstanceState.Spawn;
		}

		foreach (var pair in newStates)
		{
			var groupId = pair.Key;
			var doSpawn = pair.Value == InstanceState.Spawn;

			if (Instance.IsSpawnGroupActive(groupId) == doSpawn)
				continue; // nothing to do here

			// if we should spawn group, then spawn it...
			if (doSpawn)
				Instance.SpawnGroupSpawn(groupId, Instance);
			else // otherwise, set it as inactive so it no longer respawns (but don't despawn it)
				Instance.SetSpawnGroupInactive(groupId);
		}
	}

	void AddObject(Creature obj, bool add)
	{
		if (_creatureInfo.ContainsKey(obj.Entry))
			AddObject(obj, _creatureInfo[obj.Entry], add);
	}

	void AddObject(GameObject obj, bool add)
	{
		if (_gameObjectInfo.ContainsKey(obj.Entry))
			AddObject(obj, _gameObjectInfo[obj.Entry], add);
	}

	void AddObject(WorldObject obj, uint type, bool add)
	{
		if (add)
		{
			_objectGuids[type] = obj.GUID;
		}
		else
		{
			var guid = _objectGuids.LookupByKey(type);

			if (!guid.IsEmpty && guid == obj.GUID)
				_objectGuids.Remove(type);
		}
	}

	void DoCloseDoorOrButton(ObjectGuid guid)
	{
		if (guid.IsEmpty)
			return;

		var go = Instance.GetGameObject(guid);

		if (go)
		{
			if (go.GoType == GameObjectTypes.Door || go.GoType == GameObjectTypes.Button)
			{
				if (go.LootState == LootState.Activated)
					go.ResetDoorOrButton();
			}
			else
			{
				Log.outError(LogFilter.Scripts, "InstanceScript: DoCloseDoorOrButton can't use gameobject entry {0}, because type is {1}.", go.Entry, go.GoType);
			}
		}
		else
		{
			Log.outDebug(LogFilter.Scripts, "InstanceScript: DoCloseDoorOrButton failed");
		}
	}

	// Send Notify to all players in instance
	void DoSendNotifyToInstance(string format, params object[] args)
	{
		Instance.DoOnPlayers(player => player.Session?.SendNotification(format, args));
	}

	void SendEncounterStart(uint inCombatResCount = 0, uint maxInCombatResCount = 0, uint inCombatResChargeRecovery = 0, uint nextCombatResChargeTime = 0)
	{
		InstanceEncounterStart encounterStartMessage = new();
		encounterStartMessage.InCombatResCount = inCombatResCount;
		encounterStartMessage.MaxInCombatResCount = maxInCombatResCount;
		encounterStartMessage.CombatResChargeRecovery = inCombatResChargeRecovery;
		encounterStartMessage.NextCombatResChargeTime = nextCombatResChargeTime;

		Instance.SendToPlayers(encounterStartMessage);
	}

	void SendEncounterEnd()
	{
		Instance.SendToPlayers(new InstanceEncounterEnd());
	}

	void UpdateEncounterState(EncounterCreditType type, uint creditEntry, Unit source)
	{
		var encounters = Global.ObjectMgr.GetDungeonEncounterList(Instance.Id, Instance.DifficultyID);

		if (encounters.Empty())
			return;

		uint dungeonId = 0;

		foreach (var encounter in encounters)
			if (encounter.creditType == type && encounter.creditEntry == creditEntry)
			{
				_completedEncounters |= (1u << encounter.dbcEntry.Bit);

				if (encounter.dbcEntry.CompleteWorldStateID != 0)
					DoUpdateWorldState((uint)encounter.dbcEntry.CompleteWorldStateID, 1);

				if (encounter.lastEncounterDungeon != 0)
				{
					dungeonId = encounter.lastEncounterDungeon;

					Log.outDebug(LogFilter.Lfg,
								"UpdateEncounterState: Instance {0} (instanceId {1}) completed encounter {2}. Credit Dungeon: {3}",
								Instance.MapName,
								Instance.InstanceId,
								encounter.dbcEntry.Name[Global.WorldMgr.DefaultDbcLocale],
								dungeonId);

					break;
				}
			}

		if (dungeonId != 0)
		{
			var players = Instance.Players;

			foreach (var player in players)
			{
				var grp = player.Group;

				if (grp != null)
					if (grp.IsLFGGroup)
					{
						Global.LFGMgr.FinishDungeon(grp.GUID, dungeonId, Instance);

						return;
					}
			}
		}
	}

	void UpdatePhasing()
	{
		Instance.DoOnPlayers(player => PhasingHandler.SendToPlayer(player));
	}

	void InitializeCombatResurrections(byte charges = 1, uint interval = 0)
	{
		_combatResurrectionCharges = charges;

		if (interval == 0)
			return;

		_combatResurrectionTimer = interval;
		_combatResurrectionTimerStarted = true;
	}

	enum InstanceState
	{
		Block,
		Spawn,
		ForceBlock
	};
}