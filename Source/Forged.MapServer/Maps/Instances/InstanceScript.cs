// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.D;
using Forged.MapServer.DungeonFinding;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Events;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking.Packets.Instance;
using Forged.MapServer.Spells;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Maps.Instances;

public class InstanceScript : ZoneScript
{
    private readonly List<uint> _activatedAreaTriggers = new();
    private readonly Dictionary<uint, BossInfo> _bosses = new();
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<uint, uint> _creatureInfo = new();
    private readonly MultiMap<uint, DoorInfo> _doors = new();
    private readonly Dictionary<uint, uint> _gameObjectInfo = new();
    private readonly List<InstanceSpawnGroupInfo> _instanceSpawnGroups;
    private readonly LFGManager _lfgManager;
    private readonly Dictionary<uint, MinionInfo> _minions = new();
    private readonly Dictionary<uint, ObjectGuid> _objectGuids = new();
    private readonly GameObjectManager _objectManager;
    private readonly List<PersistentInstanceScriptValueBase> _persistentScriptValues = new();
    private readonly UnitCombatHelpers _unitCombatHelpers;
    private readonly WorldManager _worldManager;
    private readonly WorldStateManager _worldStateManager;
    private byte _combatResurrectionCharges;
    private uint _combatResurrectionTimer;

    // the counter for available battle resurrections
    private bool _combatResurrectionTimerStarted;

    private uint _completedEncounters;

    // DEPRECATED, REMOVE
    private uint _entranceId;

    private string _headers;
    private uint _temporaryEntranceId;

    public InstanceScript(InstanceMap map)
    {
        _objectManager = map.ClassFactory.Resolve<GameObjectManager>();
        _configuration = map.ClassFactory.Resolve<IConfiguration>();
        _worldStateManager = map.ClassFactory.Resolve<WorldStateManager>();
        _unitCombatHelpers = map.ClassFactory.Resolve<UnitCombatHelpers>();
        _cliDB = map.ClassFactory.Resolve<CliDB>();
        _worldManager = map.ClassFactory.Resolve<WorldManager>();
        _lfgManager = map.ClassFactory.Resolve<LFGManager>();
        Instance = map;
        _instanceSpawnGroups = _objectManager.GetInstanceSpawnGroupsForMap(map.Id);
    }

    public InstanceMap Instance { get; set; }

    public void AddCombatResurrectionCharge()
    {
        ++_combatResurrectionCharges;
        _combatResurrectionTimer = GetCombatResurrectionChargeInterval();
        _combatResurrectionTimerStarted = true;

        var gainCombatResurrectionCharge = new InstanceEncounterGainCombatResurrectionCharge
        {
            InCombatResCount = _combatResurrectionCharges,
            CombatResChargeRecovery = _combatResurrectionTimer
        };

        Instance.SendToPlayers(gainCombatResurrectionCharge);
    }

    public virtual void AddDoor(GameObject door, bool add)
    {
        if (_doors.TryGetValue(door.Entry, out var range))
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
        if (!_minions.TryGetValue(minion.Entry, out var minionInfo))
            return;

        if (add)
            minionInfo.BossInfo.Minion.Add(minion.GUID);
        else
            minionInfo.BossInfo.Minion.Remove(minion.GUID);
    }

    // Override this function to validate all additional data loads
    public virtual void AfterDataLoad() { }

    public virtual bool CheckAchievementCriteriaMeet(uint criteriaID, Player source, Unit target = null, uint miscvalue1 = 0)
    {
        Log.Logger.Error("Achievement system call CheckAchievementCriteriaMeet but instance script for map {0} not have implementation for achievement criteria {1}",
                         Instance.Id,
                         criteriaID);

        return false;
    }

    public virtual bool CheckRequiredBosses(uint bossId, Player player = null)
    {
        return true;
    }

    public virtual uint? ComputeEntranceLocationForCompletedEncounters(uint completedEncountersMask)
    {
        return null;
    }

    public virtual void Create()
    {
        for (uint i = 0; i < _bosses.Count; ++i)
            SetBossState(i, EncounterState.NotStarted);

        UpdateSpawnGroups();
    }

    public void DoCastSpellOnPlayer(Player player, uint spell, bool includePets = false, bool includeControlled = false)
    {
        if (player == null)
            return;

        player.SpellFactory.CastSpell(player, spell, true);

        if (!includePets)
            return;

        for (var i = 0; i < SharedConst.MaxSummonSlot; ++i)
        {
            var summonGUID = player.SummonSlot[i];

            if (summonGUID.IsEmpty)
                continue;

            Instance.GetCreature(summonGUID)?.SpellFactory.CastSpell(player, spell, true);
        }

        if (!includeControlled)
            return;

        foreach (var controlled in player.Controlled)
        {
            if (controlled == null)
                continue;

            if (controlled.Location.IsInWorld && controlled.IsCreature)
                controlled.SpellFactory.CastSpell(player, spell, true);
        }
    }

    // Cast spell on all players in instance
    public void DoCastSpellOnPlayers(uint spell, bool includePets = false, bool includeControlled = false)
    {
        Instance.DoOnPlayers(player => DoCastSpellOnPlayer(player, spell, includePets, includeControlled));
    }

    public void DoRemoveAurasDueToSpellOnPlayer(Player player, uint spell, bool includePets = false, bool includeControlled = false)
    {
        if (player == null)
            return;

        player.RemoveAura(spell);

        if (!includePets)
            return;

        for (var i = 0; i < SharedConst.MaxSummonSlot; ++i)
        {
            var summonGUID = player.SummonSlot[i];

            if (summonGUID.IsEmpty)
                continue;

            Instance.GetCreature(summonGUID)?.RemoveAura(spell);
        }

        if (!includeControlled)
            return;

        for (var i = 0; i < player.Controlled.Count; ++i)
        {
            var controlled = player.Controlled[i];

            if (controlled == null)
                continue;

            if (controlled.Location.IsInWorld && controlled.IsCreature)
                controlled.RemoveAura(spell);
        }
    }

    // Remove Auras due to Spell on all players in instance
    public void DoRemoveAurasDueToSpellOnPlayers(uint spell, bool includePets = false, bool includeControlled = false)
    {
        Instance.DoOnPlayers(player => DoRemoveAurasDueToSpellOnPlayer(player, spell, includePets, includeControlled));
    }

    public void DoRespawnGameObject(ObjectGuid guid, TimeSpan timeToDespawn)
    {
        var go = Instance.GetGameObject(guid);

        if (go != null)
        {
            switch (go.GoType)
            {
                case GameObjectTypes.Door:
                case GameObjectTypes.Button:
                case GameObjectTypes.Trap:
                case GameObjectTypes.FishingNode:
                    // not expect any of these should ever be handled
                    Log.Logger.Error("InstanceScript: DoRespawnGameObject can't respawn gameobject entry {0}, because type is {1}.", go.Entry, go.GoType);

                    return;
            }

            if (go.IsSpawned)
                return;

            go.SetRespawnTime((int)timeToDespawn.TotalSeconds);
        }
        else
            Log.Logger.Debug("InstanceScript: DoRespawnGameObject failed");
    }

    // Update Achievement Criteria for all players in instance
    public void DoUpdateCriteria(CriteriaType type, uint miscValue1 = 0, uint miscValue2 = 0, Unit unit = null)
    {
        Instance.DoOnPlayers(player => player.UpdateCriteria(type, miscValue1, miscValue2, 0, unit));
    }

    public void DoUpdateWorldState(uint worldStateId, int value)
    {
        _worldStateManager.SetValue(worldStateId, value, false, Instance);
    }

    public void DoUseDoorOrButton(ObjectGuid uiGuid, uint withRestoreTime = 0, bool useAlternativeState = false)
    {
        if (uiGuid.IsEmpty)
            return;

        var go = Instance.GetGameObject(uiGuid);

        if (go != null)
        {
            if (go.GoType is GameObjectTypes.Door or GameObjectTypes.Button)
            {
                if (go.LootState == LootState.Ready)
                    go.UseDoorOrButton(withRestoreTime, useAlternativeState);
                else if (go.LootState == LootState.Activated)
                    go.ResetDoorOrButton();
            }
            else
                Log.Logger.Error("InstanceScript: DoUseDoorOrButton can't use gameobject entry {0}, because type is {1}.", go.Entry, go.GoType);
        }
        else
            Log.Logger.Debug("InstanceScript: DoUseDoorOrButton failed");
    }

    public List<AreaBoundary> GetBossBoundary(uint id)
    {
        return id < _bosses.Count ? _bosses[id].Boundary : null;
    }

    public DungeonEncounterRecord GetBossDungeonEncounter(uint id)
    {
        return id < _bosses.Count ? _bosses[id].GetDungeonEncounterForDifficulty(Instance.DifficultyID) : null;
    }

    public DungeonEncounterRecord GetBossDungeonEncounter(Creature creature)
    {
        if (creature.AI is BossAI bossAi)
            return GetBossDungeonEncounter(bossAi.GetBossId());

        return null;
    }

    public BossInfo GetBossInfo(uint id)
    {
        return _bosses[id];
    }

    public EncounterState GetBossState(uint id)
    {
        return id < _bosses.Count ? _bosses[id].State : EncounterState.ToBeDecided;
    }

    public uint GetCombatResurrectionChargeInterval()
    {
        uint interval = 0;
        var playerCount = Instance.Players.Count;

        if (playerCount != 0)
            interval = (uint)(90 * Time.MINUTE * Time.IN_MILLISECONDS / playerCount);

        return interval;
    }

    public byte GetCombatResurrectionCharges()
    {
        return _combatResurrectionCharges;
    }

    public uint GetCompletedEncounterMask()
    {
        return _completedEncounters;
    }

    public Creature GetCreature(uint type)
    {
        return Instance.GetCreature(GetObjectGuid(type));
    }

    public int GetEncounterCount()
    {
        return _bosses.Count;
    }

    // Get's the current entrance id
    public uint GetEntranceLocation()
    {
        return _temporaryEntranceId != 0 ? _temporaryEntranceId : _entranceId;
    }

    public uint? GetEntranceLocationForCompletedEncounters(uint completedEncountersMask)
    {
        return !Instance.MapDifficulty.IsUsingEncounterLocks() ? _entranceId : ComputeEntranceLocationForCompletedEncounters(completedEncountersMask);
    }

    public GameObject GetGameObject(uint type)
    {
        return Instance.GetGameObject(GetObjectGuid(type));
    }

    public override ObjectGuid GetGuidData(uint type)
    {
        return GetObjectGuid(type);
    }

    public string GetHeader()
    {
        return _headers;
    }

    public List<InstanceSpawnGroupInfo> GetInstanceSpawnGroups()
    {
        return _instanceSpawnGroups;
    }

    public ObjectGuid GetObjectGuid(uint type)
    {
        return _objectGuids.LookupByKey(type);
    }

    public List<PersistentInstanceScriptValueBase> GetPersistentScriptValues()
    {
        return _persistentScriptValues;
    }

    public string GetSaveData()
    {
        OutSaveInstData();

        InstanceScriptDataWriter writer = new(this);

        writer.FillData();

        OutSaveInstDataComplete();

        return writer.GetString();
    }

    public void HandleGameObject(ObjectGuid guid, bool open, GameObject go = null)
    {
        go ??= Instance.GetGameObject(guid);

        if (go != null)
            go.SetGoState(open ? GameObjectState.Active : GameObjectState.Ready);
        else
            Log.Logger.Debug("InstanceScript: HandleGameObject failed");
    }

    public bool InstanceHasScript(WorldObject obj, string scriptName)
    {
        var instance = obj.Location.Map.ToInstanceMap;

        if (instance != null)
            return instance.GetScriptName() == scriptName;

        return false;
    }

    public bool IsAreaTriggerDone(uint id)
    {
        return _activatedAreaTriggers.Contains(id);
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

        if (dungeonEncounter == null)
            return false;

        if ((completedEncountersMask & (1 << dungeonEncounter.Bit)) != 0)
            return _bosses[bossId].State == EncounterState.Done;

        return false;
    }

    public virtual bool IsEncounterInProgress()
    {
        return _bosses.Values.Any(boss => boss.State == EncounterState.InProgress);
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
            OutLoadInstDataFail();

        OutLoadInstDataComplete();
    }

    public void LoadBossBoundaries(BossBoundaryEntry[] data)
    {
        foreach (var entry in data)
            if (entry.BossId < _bosses.Count)
                _bosses[entry.BossId].Boundary.Add(entry.Boundary);
    }

    public void LoadDoorData(params DoorData[] data)
    {
        foreach (var door in data)
        {
            if (door.Entry == 0)
                continue;

            if (door.BossId < _bosses.Count)
                _doors.Add(door.Entry, new DoorInfo(_bosses[door.BossId], door.Type));
        }

        Log.Logger.Debug("InstanceScript.LoadDoorData: {0} doors loaded.", _doors.Count);
    }

    public void LoadDungeonEncounterData(DungeonEncounterData[] encounters)
    {
        foreach (var encounter in encounters)
            LoadDungeonEncounterData(encounter.BossId, encounter.DungeonEncounterId);
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

        Log.Logger.Debug("InstanceScript.LoadMinionData: {0} minions loaded.", _minions.Count);
    }

    public void LoadObjectData(ObjectData[] creatureData, ObjectData[] gameObjectData)
    {
        if (creatureData != null)
            LoadObjectData(creatureData, _creatureInfo);

        if (gameObjectData != null)
            LoadObjectData(gameObjectData, _gameObjectInfo);

        Log.Logger.Debug("InstanceScript.LoadObjectData: {0} objects loaded.", _creatureInfo.Count + _gameObjectInfo.Count);
    }

    // Only used by areatriggers that inherit from OnlyOnceAreaTriggerScript
    public void MarkAreaTriggerDone(uint id)
    {
        _activatedAreaTriggers.Add(id);
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

    // Called when a player successfully enters the instance.
    public virtual void OnPlayerEnter(Player player) { }

    // Called when a player successfully leaves the instance.
    public virtual void OnPlayerLeave(Player player) { }

    public void OutLoadInstData(string input)
    {
        Log.Logger.Debug("Loading Instance Data for Instance {0} (Map {1}, Instance Id {2}). Input is '{3}'", Instance.MapName, Instance.Id, Instance.InstanceId, input);
    }

    public void OutLoadInstDataComplete()
    {
        Log.Logger.Debug("Instance Data Load for Instance {0} (Map {1}, Instance Id: {2}) is complete.", Instance.MapName, Instance.Id, Instance.InstanceId);
    }

    public void OutLoadInstDataFail()
    {
        Log.Logger.Debug("Unable to load Instance Data for Instance {0} (Map {1}, Instance Id: {2}).", Instance.MapName, Instance.Id, Instance.InstanceId);
    }

    public void OutSaveInstData()
    {
        Log.Logger.Debug("Saving Instance Data for Instance {0} (Map {1}, Instance Id {2})", Instance.MapName, Instance.Id, Instance.InstanceId);
    }

    public void OutSaveInstDataComplete()
    {
        Log.Logger.Debug("Saving Instance Data for Instance {0} (Map {1}, Instance Id {2}) completed.", Instance.MapName, Instance.Id, Instance.InstanceId);
    }

    public void RegisterPersistentScriptValue(PersistentInstanceScriptValueBase value)
    {
        _persistentScriptValues.Add(value);
    }

    public void ResetAreaTriggerDone(uint id)
    {
        _activatedAreaTriggers.Remove(id);
    }

    public void ResetCombatResurrections()
    {
        _combatResurrectionCharges = 0;
        _combatResurrectionTimer = 0;
        _combatResurrectionTimerStarted = false;
    }

    public void SendBossKillCredit(uint encounterId)
    {
        BossKill bossKillCreditMessage = new()
        {
            DungeonEncounterID = encounterId
        };

        Instance.SendToPlayers(bossKillCreditMessage);
    }

    public void SendEncounterUnit(EncounterFrameType type, Unit unit = null, byte priority = 0)
    {
        switch (type)
        {
            case EncounterFrameType.Engage:
                if (unit == null)
                    return;

                InstanceEncounterEngageUnit encounterEngageMessage = new()
                {
                    Unit = unit.GUID,
                    TargetFramePriority = priority
                };

                Instance.SendToPlayers(encounterEngageMessage);

                break;

            case EncounterFrameType.Disengage:
                if (unit == null)
                    return;

                InstanceEncounterDisengageUnit encounterDisengageMessage = new()
                {
                    Unit = unit.GUID
                };

                Instance.SendToPlayers(encounterDisengageMessage);

                break;

            case EncounterFrameType.UpdatePriority:
                if (unit == null)
                    return;

                InstanceEncounterChangePriority encounterChangePriorityMessage = new()
                {
                    Unit = unit.GUID,
                    TargetFramePriority = priority
                };

                Instance.SendToPlayers(encounterChangePriorityMessage);

                break;
        }
    }

    // Return wether server allow two side groups or not
    public bool ServerAllowsTwoSideGroups()
    {
        return _configuration.GetDefaultValue("AllowTwoSide:Interaction:Group", false);
    }

    public void SetBossNumber(uint number)
    {
        for (uint i = 0; i < number; ++i)
            _bosses.Add(i, new BossInfo());
    }

    public virtual bool SetBossState(uint id, EncounterState state)
    {
        if (id < _bosses.Count)
        {
            var bossInfo = _bosses[id];

            if (bossInfo.State == EncounterState.ToBeDecided) // loading
            {
                bossInfo.State = state;
                Log.Logger.Debug($"InstanceScript: Initialize boss {id} state as {state} (map {Instance.Id}, {Instance.InstanceId}).");

                return false;
            }

            if (bossInfo.State == state)
                return false;

            if (bossInfo.State == EncounterState.Done)
            {
                Log.Logger.Error($"InstanceScript: Tried to set instance boss {id} state from {bossInfo.State} back to {state} for map {Instance.Id}, instance id {Instance.InstanceId}. Blocked!");

                return false;
            }

            if (state == EncounterState.Done)
                foreach (var guid in bossInfo.Minion)
                {
                    var minion = Instance.GetCreature(guid);

                    if (minion == null)
                        continue;

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
                            _unitCombatHelpers.ProcSkillsAndAuras(player, null, new ProcFlagsInit(ProcFlags.EncounterStart), new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
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
            }

            bossInfo.State = state;

            if (dungeonEncounter != null)
                Instance.UpdateInstanceLock(new UpdateBossStateSaveDataEvent(dungeonEncounter, id, state));

            for (uint type = 0; type < (int)DoorType.Max; ++type)
                foreach (var guid in bossInfo.Door[type])
                {
                    var door = Instance.GetGameObject(guid);

                    if (door != null)
                        UpdateDoorState(door);
                }

            foreach (var guid in bossInfo.Minion.ToList())
            {
                var minion = Instance.GetCreature(guid);

                if (minion != null)
                    UpdateMinionState(minion, state);
            }

            UpdateSpawnGroups();

            return true;
        }

        return false;
    }

    public void SetCompletedEncountersMask(uint newMask)
    {
        _completedEncounters = newMask;

        var encounters = _objectManager.GetDungeonEncounterList(Instance.Id, Instance.DifficultyID);

        if (encounters == null)
            return;

        foreach (var encounter in encounters)
            if ((_completedEncounters & (1 << encounter.DBCEntry.Bit)) != 0 && encounter.DBCEntry.CompleteWorldStateID != 0)
                DoUpdateWorldState((uint)encounter.DBCEntry.CompleteWorldStateID, 1);
    }

    public void SetEntranceLocation(uint worldSafeLocationId)
    {
        _entranceId = worldSafeLocationId;
        _temporaryEntranceId = 0;
    }

    public void SetHeaders(string dataHeaders)
    {
        _headers = dataHeaders;
    }

    // Sets a temporary entrance that does not get saved to db
    public void SetTemporaryEntranceLocation(uint worldSafeLocationId)
    {
        _temporaryEntranceId = worldSafeLocationId;
    }

    public bool SkipCheckRequiredBosses(Player player = null)
    {
        return player != null && player.Session.HasPermission(RBACPermissions.SkipCheckInstanceRequiredBosses);
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

        ProcessEvent(target, gameEventId, null);
        Instance.DoOnPlayers(player => GameEvents.TriggerForPlayer(gameEventId, player));

        GameEvents.TriggerForMap(gameEventId, Instance);
    }

    public virtual void Update(uint diff) { }

    public string UpdateAdditionalSaveData(string oldData, UpdateAdditionalSaveDataEvent saveEvent)
    {
        if (!Instance.MapDifficulty.IsUsingEncounterLocks())
            return GetSaveData();

        InstanceScriptDataWriter writer = new(this);
        writer.FillDataFrom(oldData);
        writer.SetAdditionalData(saveEvent);

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

    public void UpdateCombatResurrection(uint diff)
    {
        if (!_combatResurrectionTimerStarted)
            return;

        if (_combatResurrectionTimer <= diff)
            AddCombatResurrectionCharge();
        else
            _combatResurrectionTimer -= diff;
    }

    public virtual void UpdateDoorState(GameObject door)
    {
        if (_doors.TryGetValue(door.Entry, out var range))
            return;

        var open = true;

        foreach (var info in range)
        {
            if (!open)
                break;

            open = info.Type switch
            {
                DoorType.Room      => info.BossInfo.State != EncounterState.InProgress,
                DoorType.Passage   => info.BossInfo.State == EncounterState.Done,
                DoorType.SpawnHole => info.BossInfo.State == EncounterState.InProgress,
                _                  => true
            };
        }

        door.SetGoState(open ? GameObjectState.Active : GameObjectState.Ready);
    }

    public void UpdateEncounterStateForKilledCreature(uint creatureId, Unit source)
    {
        UpdateEncounterState(EncounterCreditType.KillCreature, creatureId);
    }

    public void UpdateEncounterStateForSpellCast(uint spellId, Unit source)
    {
        UpdateEncounterState(EncounterCreditType.CastSpell, spellId);
    }

    public void UseCombatResurrection()
    {
        --_combatResurrectionCharges;

        Instance.SendToPlayers(new InstanceEncounterInCombatResurrection());
    }

    private void AddObject(Creature obj, bool add)
    {
        if (_creatureInfo.ContainsKey(obj.Entry))
            AddObject(obj, _creatureInfo[obj.Entry], add);
    }

    private void AddObject(GameObject obj, bool add)
    {
        if (_gameObjectInfo.ContainsKey(obj.Entry))
            AddObject(obj, _gameObjectInfo[obj.Entry], add);
    }

    private void AddObject(WorldObject obj, uint type, bool add)
    {
        if (add)
            _objectGuids[type] = obj.GUID;
        else
        {
            var guid = _objectGuids.LookupByKey(type);

            if (!guid.IsEmpty && guid == obj.GUID)
                _objectGuids.Remove(type);
        }
    }

    private void InitializeCombatResurrections(byte charges = 1, uint interval = 0)
    {
        _combatResurrectionCharges = charges;

        if (interval == 0)
            return;

        _combatResurrectionTimer = interval;
        _combatResurrectionTimerStarted = true;
    }

    private void LoadDungeonEncounterData(uint bossId, uint[] dungeonEncounterIds)
    {
        if (bossId >= _bosses.Count)
            return;

        for (var i = 0; i < dungeonEncounterIds.Length && i < MapConst.MaxDungeonEncountersPerBoss; ++i)
            _bosses[bossId].DungeonEncounters[i] = _cliDB.DungeonEncounterStorage.LookupByKey(dungeonEncounterIds[i]);
    }

    private void LoadObjectData(ObjectData[] objectData, Dictionary<uint, uint> objectInfo)
    {
        foreach (var data in objectData)
            objectInfo[data.Entry] = data.Type;
    }

    private void SendEncounterEnd()
    {
        Instance.SendToPlayers(new InstanceEncounterEnd());
    }

    private void SendEncounterStart(uint inCombatResCount = 0, uint maxInCombatResCount = 0, uint inCombatResChargeRecovery = 0, uint nextCombatResChargeTime = 0)
    {
        InstanceEncounterStart encounterStartMessage = new()
        {
            InCombatResCount = inCombatResCount,
            MaxInCombatResCount = maxInCombatResCount,
            CombatResChargeRecovery = inCombatResChargeRecovery,
            NextCombatResChargeTime = nextCombatResChargeTime
        };

        Instance.SendToPlayers(encounterStartMessage);
    }

    private void UpdateEncounterState(EncounterCreditType type, uint creditEntry)
    {
        var encounters = _objectManager.GetDungeonEncounterList(Instance.Id, Instance.DifficultyID);

        if (encounters.Empty())
            return;

        uint dungeonId = 0;

        foreach (var encounter in encounters)
            if (encounter.CreditType == type && encounter.CreditEntry == creditEntry)
            {
                _completedEncounters |= 1u << encounter.DBCEntry.Bit;

                if (encounter.DBCEntry.CompleteWorldStateID != 0)
                    DoUpdateWorldState((uint)encounter.DBCEntry.CompleteWorldStateID, 1);

                if (encounter.LastEncounterDungeon == 0)
                    continue;

                dungeonId = encounter.LastEncounterDungeon;

                Log.Logger.Debug("UpdateEncounterState: Instance {0} (instanceId {1}) completed encounter {2}. Credit Dungeon: {3}",
                                 Instance.MapName,
                                 Instance.InstanceId,
                                 encounter.DBCEntry.Name[_worldManager.DefaultDbcLocale],
                                 dungeonId);

                break;
            }

        if (dungeonId == 0)
            return;

        var players = Instance.Players;

        foreach (var player in players.Where(player => player.Group is { IsLFGGroup: true }))
        {
            _lfgManager.FinishDungeon(player.Group.GUID, dungeonId, Instance);

            return;
        }
    }

    private void UpdateMinionState(Creature minion, EncounterState state)
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
        }
    }

    private void UpdateSpawnGroups()
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

            if ((Instance.TeamIdInInstance == TeamIds.Alliance && info.Flags.HasFlag(InstanceSpawnGroupFlags.HordeOnly)) || (Instance.TeamIdInInstance == TeamIds.Horde && info.Flags.HasFlag(InstanceSpawnGroupFlags.AllianceOnly)))
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
                Instance.SpawnGroupSpawn(groupId);
            else // otherwise, set it as inactive so it no longer respawns (but don't despawn it)
                Instance.SetSpawnGroupInactive(groupId);
        }
    }

    private enum InstanceState
    {
        Block,
        Spawn,
        ForceBlock
    };
}