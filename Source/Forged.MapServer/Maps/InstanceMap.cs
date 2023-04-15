// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Networking.Packets.Instance;
using Forged.MapServer.Scenarios;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IMap;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Maps;

public class InstanceMap : Map
{
    private readonly IConfiguration _configuration;
    private readonly WorldStateManager _worldStateManager;
    private readonly WorldManager _worldManager;
    private readonly GameObjectManager _gameObjectManager;
    private readonly ScriptManager _scriptManager;
    private readonly InstanceLockManager _instanceLockManager;
    private readonly CharacterDatabase _characterDatabase;
    private readonly GroupInstanceReference _owningGroupRef = new();
    private DateTime? _instanceExpireEvent;

    public InstanceMap(uint id, long expiry, uint instanceId, Difficulty spawnMode, int instanceTeam, InstanceLock instanceLock, ClassFactory classFactory, IConfiguration configuration,
                       WorldStateManager worldStateManager, WorldManager worldManager, GameObjectManager gameObjectManager, ScriptManager scriptManager, InstanceLockManager instanceLockManager,
                       CharacterDatabase characterDatabase) 
        : base(id, expiry, instanceId, spawnMode, classFactory)
    {
        _configuration = configuration;
        _worldStateManager = worldStateManager;
        _worldManager = worldManager;
        _gameObjectManager = gameObjectManager;
        _scriptManager = scriptManager;
        _instanceLockManager = instanceLockManager;
        _characterDatabase = characterDatabase;
        InstanceLock = instanceLock;
        
        //lets initialize visibility distance for dungeons
        InitVisibilityDistance();

        // the timer is started by default, and stopped when the first player joins
        // this make sure it gets unloaded if for some reason no player joins
        UnloadTimer = Math.Max(_configuration.GetDefaultValue("Instance.UnloadDelay", 30u * Time.MINUTE * Time.IN_MILLISECONDS), 1);

        _worldStateManager.SetValue(WorldStates.TeamInInstanceAlliance, instanceTeam == TeamIds.Alliance ? 1 : 0, false, this);
        _worldStateManager.SetValue(WorldStates.TeamInInstanceHorde, instanceTeam == TeamIds.Horde ? 1 : 0, false, this);

        if (InstanceLock == null)
            return;

        InstanceLock.SetInUse(true);
        _instanceExpireEvent = InstanceLock.GetExpiryTime(); // ignore extension state for reset event (will ask players to accept extended save on expiration)
    }

    ~InstanceMap()
    {
        InstanceLock?.SetInUse(false);
    }

    public InstanceLock InstanceLock { get; }

    public InstanceScenario InstanceScenario { get; private set; }

    public InstanceScript InstanceScript { get; private set; }

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

    public uint ScriptId { get; private set; }

    public int TeamIdInInstance
    {
        get
        {
            if (_worldStateManager.GetValue(WorldStates.TeamInInstanceAlliance, this) != 0)
                return TeamIds.Alliance;

            return _worldStateManager.GetValue(WorldStates.TeamInInstanceHorde, this) != 0 ? TeamIds.Horde : TeamIds.Neutral;
        }
    }

    public TeamFaction TeamInInstance => TeamIdInInstance == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde;

    public override bool AddPlayerToMap(Player player, bool initPlayer = true)
    {
        // increase current instances (hourly limit)
        player.AddInstanceEnterTime(InstanceId, GameTime.CurrentTime);

        MapDb2Entries entries = new(Entry, MapDifficulty);

        if (entries.MapDifficulty.HasResetSchedule() && InstanceLock != null && InstanceLock.GetData().CompletedEncountersMask != 0)
            if (!entries.MapDifficulty.IsUsingEncounterLocks())
            {
                var playerLock = _instanceLockManager.FindActiveInstanceLock(player.GUID, entries);

                if (playerLock == null ||
                    (playerLock.IsExpired() && playerLock.IsExtended()) ||
                    playerLock.GetData().CompletedEncountersMask != InstanceLock.GetData().CompletedEncountersMask)
                {
                    PendingRaidLock pendingRaidLock = new()
                    {
                        TimeUntilLock = 60000,
                        CompletedMask = InstanceLock.GetData().CompletedEncountersMask,
                        Extending = playerLock != null && playerLock.IsExtended(),
                        WarningOnly = entries.Map.IsFlexLocking() // events it triggers:  1 : INSTANCE_LOCK_WARNING   0 : INSTANCE_LOCK_STOP / INSTANCE_LOCK_START
                    };

                    player.Session.SendPacket(pendingRaidLock);

                    if (!entries.Map.IsFlexLocking())
                        player.SetPendingBind(InstanceId, 60000);
                }
            }

        Log.Logger.Information("MAP: Player '{0}' entered instance '{1}' of map '{2}'",
                               player.GetName(),
                               InstanceId,
                               MapName);

        // initialize unload state
        UnloadTimer = 0;

        // this will acquire the same mutex so it cannot be in the previous block
        base.AddPlayerToMap(player, initPlayer);

        InstanceScript?.OnPlayerEnter(player);

        InstanceScenario?.OnPlayerEnter(player);

        return true;
    }

    public override TransferAbortParams CannotEnter(Player player)
    {
        if (player.Location.Map == this)
        {
            Log.Logger.Error("InstanceMap:CannotEnter - player {0} ({1}) already in map {2}, {3}, {4}!", player.GetName(), player.GUID.ToString(), Id, InstanceId, DifficultyID);

            return new TransferAbortParams(TransferAbortReason.Error);
        }

        // allow GM's to enter
        if (player.IsGameMaster)
            return base.CannotEnter(player);

        // cannot enter if the instance is full (player cap), GMs don't count
        var maxPlayers = MaxPlayers;

        if (GetPlayersCountExceptGMs() >= maxPlayers)
        {
            Log.Logger.Information("MAP: Instance '{0}' of map '{1}' cannot have more than '{2}' players. Player '{3}' rejected", InstanceId, MapName, maxPlayers, player.GetName());

            return new TransferAbortParams(TransferAbortReason.MaxPlayers);
        }

        // cannot enter while an encounter is in progress (unless this is a relog, in which case it is permitted)
        if (!player.IsLoading && IsRaid && InstanceScript != null && InstanceScript.IsEncounterInProgress())
            return new TransferAbortParams(TransferAbortReason.ZoneInCombat);

        if (InstanceLock == null)
            return base.CannotEnter(player);

        // cannot enter if player is permanent saved to a different instance id
        var lockError = _instanceLockManager.CanJoinInstanceLock(player.GUID, new MapDb2Entries(Entry, MapDifficulty), InstanceLock);

        return lockError != TransferAbortReason.None ? new TransferAbortParams(lockError) : base.CannotEnter(player);
    }

    public void CreateInstanceData()
    {
        if (InstanceScript != null)
            return;

        var mInstance = _gameObjectManager.GetInstanceTemplate(Id);

        if (mInstance != null)
        {
            ScriptId = mInstance.ScriptId;
            InstanceScript = _scriptManager.RunScriptRet<IInstanceMapGetInstanceScript, InstanceScript>(p => p.GetInstanceScript(this), ScriptId, null);
        }

        if (InstanceScript == null)
            return;

        if (InstanceLock == null || InstanceLock.GetInstanceId() == 0)
        {
            InstanceScript.Create();

            return;
        }

        MapDb2Entries entries = new(Entry, MapDifficulty);

        if (!entries.IsInstanceIdBound() || !IsRaid || !entries.MapDifficulty.IsRestoringDungeonState() || _owningGroupRef.IsValid())
        {
            InstanceScript.Create();

            return;
        }

        var lockData = InstanceLock.GetInstanceInitializationData();
        InstanceScript.SetCompletedEncountersMask(lockData.CompletedEncountersMask);
        InstanceScript.SetEntranceLocation(lockData.EntranceWorldSafeLocId);

        if (!lockData.Data.IsEmpty())
        {
            Log.Logger.Debug($"Loading instance data for `{_gameObjectManager.GetScriptName(ScriptId)}` with id {InstanceIdInternal}");
            InstanceScript.Load(lockData.Data);
        }
        else
        {
            InstanceScript.Create();
        }
    }

    public void CreateInstanceLockForPlayer(Player player)
    {
        MapDb2Entries entries = new(Entry, MapDifficulty);
        var playerLock = _instanceLockManager.FindActiveInstanceLock(player.GUID, entries);

        var isNewLock = playerLock == null || playerLock.GetData().CompletedEncountersMask == 0 || playerLock.IsExpired();

        SQLTransaction trans = new();

        var newLock = _instanceLockManager.UpdateInstanceLockForPlayer(trans, player.GUID, entries, new InstanceLockUpdateEvent(InstanceId, InstanceScript.GetSaveData(), InstanceLock.GetData().CompletedEncountersMask, null, null));

        _characterDatabase.CommitTransaction(trans);

        if (!isNewLock)
            return;

        InstanceSaveCreated data = new()
        {
            Gm = player.IsGameMaster
        };

        player.SendPacket(data);

        player.Session.SendCalendarRaidLockoutAdded(newLock);
    }

    public override string GetDebugInfo()
    {
        return $"{base.GetDebugInfo()}\nScriptId: {ScriptId} ScriptName: {GetScriptName()}";
    }

    public PlayerGroup GetOwningGroup()
    {
        return _owningGroupRef.Target;
    }

    public string GetScriptName()
    {
        return _gameObjectManager.GetScriptName(ScriptId);
    }

    public override void InitVisibilityDistance()
    {
        //init visibility distance for instances
        VisibleDistance = _worldManager.MaxVisibleDistanceInInstances;
        VisibilityNotifyPeriod = _worldManager.VisibilityNotifyPeriodInInstances;
    }
    public override void RemovePlayerFromMap(Player player, bool remove)
    {
        Log.Logger.Information("MAP: Removing player '{0}' from instance '{1}' of map '{2}' before relocating to another map", player.GetName(), InstanceId, MapName);

        InstanceScript?.OnPlayerLeave(player);

        // if last player set unload timer
        if (UnloadTimer == 0 && Players.Count == 1)
            UnloadTimer = InstanceLock != null && InstanceLock.IsExpired() ? 1 : Math.Max(_configuration.GetDefaultValue("Instance.UnloadDelay", 30u * Time.MINUTE * Time.IN_MILLISECONDS), 1);

        InstanceScenario?.OnPlayerExit(player);

        base.RemovePlayerFromMap(player, remove);
    }

    public InstanceResetResult Reset(InstanceResetMethod method)
    {
        // raids can be reset if no boss was killed
        if (method != InstanceResetMethod.Expire && InstanceLock != null && InstanceLock.GetData().CompletedEncountersMask != 0)
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
                    RaidInstanceMessage raidInstanceMessage = new()
                    {
                        Type = InstanceResetWarningType.Expired,
                        MapID = Id,
                        DifficultyID = DifficultyID
                    };

                    raidInstanceMessage.Write();

                    PendingRaidLock pendingRaidLock = new()
                    {
                        TimeUntilLock = 60000,
                        CompletedMask = InstanceLock.GetData().CompletedEncountersMask,
                        Extending = true,
                        WarningOnly = Entry.IsFlexLocking()
                    };

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

    public void SetInstanceScenario(InstanceScenario scenario)
    {
        InstanceScenario = scenario;
    }

    public void TrySetOwningGroup(PlayerGroup group)
    {
        if (!_owningGroupRef.IsValid())
            _owningGroupRef.Link(group, this);
    }

    public override void Update(uint diff)
    {
        base.Update(diff);

        if (InstanceScript != null)
        {
            InstanceScript.Update(diff);
            InstanceScript.UpdateCombatResurrection(diff);
        }

        InstanceScenario?.Update(diff);

        if (!_instanceExpireEvent.HasValue || _instanceExpireEvent.Value >= GameTime.SystemTime)
            return;

        Reset(InstanceResetMethod.Expire);
        _instanceExpireEvent = _instanceLockManager.GetNextResetTime(new MapDb2Entries(Entry, MapDifficulty));
    }
    public void UpdateInstanceLock(UpdateBossStateSaveDataEvent updateSaveDataEvent)
    {
        if (InstanceLock == null)
            return;

        var instanceCompletedEncounters = InstanceLock.GetData().CompletedEncountersMask | (1u << updateSaveDataEvent.DungeonEncounter.Bit);

        MapDb2Entries entries = new(Entry, MapDifficulty);

        SQLTransaction trans = new();

        if (entries.IsInstanceIdBound())
            _instanceLockManager.UpdateSharedInstanceLock(trans,
                                                          new InstanceLockUpdateEvent(InstanceId,
                                                                                      InstanceScript.GetSaveData(),
                                                                                      instanceCompletedEncounters,
                                                                                      updateSaveDataEvent.DungeonEncounter,
                                                                                      InstanceScript.GetEntranceLocationForCompletedEncounters(instanceCompletedEncounters)));

        foreach (var player in Players)
        {
            // never instance bind GMs with GM mode enabled
            if (player.IsGameMaster)
                continue;

            var playerLock = _instanceLockManager.FindActiveInstanceLock(player.GUID, entries);
            var oldData = "";
            uint playerCompletedEncounters = 0;

            if (playerLock != null)
            {
                oldData = playerLock.GetData().Data;
                playerCompletedEncounters = playerLock.GetData().CompletedEncountersMask | (1u << updateSaveDataEvent.DungeonEncounter.Bit);
            }

            var isNewLock = playerLock == null || playerLock.GetData().CompletedEncountersMask == 0 || playerLock.IsExpired();

            var newLock = _instanceLockManager.UpdateInstanceLockForPlayer(trans,
                                                                           player.GUID,
                                                                           entries,
                                                                           new InstanceLockUpdateEvent(InstanceId,
                                                                                                       InstanceScript.UpdateBossStateSaveData(oldData, updateSaveDataEvent),
                                                                                                       instanceCompletedEncounters,
                                                                                                       updateSaveDataEvent.DungeonEncounter,
                                                                                                       InstanceScript.GetEntranceLocationForCompletedEncounters(playerCompletedEncounters)));

            if (!isNewLock)
                continue;

            InstanceSaveCreated data = new()
            {
                Gm = player.IsGameMaster
            };

            player.SendPacket(data);

            player.Session.SendCalendarRaidLockoutAdded(newLock);
        }

        _characterDatabase.CommitTransaction(trans);
    }

    public void UpdateInstanceLock(UpdateAdditionalSaveDataEvent updateSaveDataEvent)
    {
        if (InstanceLock != null)
        {
            var instanceCompletedEncounters = InstanceLock.GetData().CompletedEncountersMask;

            MapDb2Entries entries = new(Entry, MapDifficulty);

            SQLTransaction trans = new();

            if (entries.IsInstanceIdBound())
                _instanceLockManager.UpdateSharedInstanceLock(trans, new InstanceLockUpdateEvent(InstanceId, InstanceScript.GetSaveData(), instanceCompletedEncounters, null, null));

            foreach (var player in Players)
            {
                // never instance bind GMs with GM mode enabled
                if (player.IsGameMaster)
                    continue;

                var playerLock = _instanceLockManager.FindActiveInstanceLock(player.GUID, entries);
                var oldData = "";

                if (playerLock != null)
                    oldData = playerLock.GetData().Data;

                var isNewLock = playerLock == null || playerLock.GetData().CompletedEncountersMask == 0 || playerLock.IsExpired();

                var newLock = _instanceLockManager.UpdateInstanceLockForPlayer(trans,
                                                                                 player.GUID,
                                                                                 entries,
                                                                                 new InstanceLockUpdateEvent(InstanceId,
                                                                                                             InstanceScript.UpdateAdditionalSaveData(oldData, updateSaveDataEvent),
                                                                                                             instanceCompletedEncounters,
                                                                                                             null,
                                                                                                             null));

                if (!isNewLock)
                    continue;

                InstanceSaveCreated data = new()
                {
                    Gm = player.IsGameMaster
                };

                player.SendPacket(data);

                player.Session.SendCalendarRaidLockoutAdded(newLock);
            }

            _characterDatabase.CommitTransaction(trans);
        }
    }
}