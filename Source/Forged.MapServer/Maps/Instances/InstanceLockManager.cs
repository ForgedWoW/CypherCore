// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Maps.Instances;

using InstanceLockKey = Tuple<uint, uint>;

public class InstanceLockManager
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly DB2Manager _db2Manager;
    private readonly Dictionary<uint, SharedInstanceLockData> _instanceLockDataById = new();
    private readonly Dictionary<ObjectGuid, Dictionary<InstanceLockKey, InstanceLock>> _instanceLocksByPlayer = new();
    private readonly object _lockObject = new();
    private readonly MapManager _mapManager;
    private readonly Dictionary<ObjectGuid, Dictionary<InstanceLockKey, InstanceLock>> _temporaryInstanceLocksByPlayer = new();
    private readonly WorldManager _worldManager;

    // locks stored here before any boss gets killed
    private bool _unloading;

    public InstanceLockManager(CharacterDatabase characterDatabase, IConfiguration configuration, WorldManager worldManager, CliDB cliDB, MapManager mapManager, DB2Manager db2Manager)
    {
        _characterDatabase = characterDatabase;
        _configuration = configuration;
        _worldManager = worldManager;
        _cliDB = cliDB;
        _mapManager = mapManager;
        _db2Manager = db2Manager;
    }

    public TransferAbortReason CanJoinInstanceLock(ObjectGuid playerGuid, MapDb2Entries entries, InstanceLock instanceLock)
    {
        if (!entries.MapDifficulty.HasResetSchedule())
            return TransferAbortReason.None;

        var playerInstanceLock = FindActiveInstanceLock(playerGuid, entries);

        if (playerInstanceLock == null)
            return TransferAbortReason.None;

        if (entries.Map.IsFlexLocking())
            // compare completed encounters - if instance has any encounters unkilled in players lock then cannot enter
            return (playerInstanceLock.Data.CompletedEncountersMask & ~instanceLock.Data.CompletedEncountersMask) != 0 ? TransferAbortReason.AlreadyCompletedEncounter : TransferAbortReason.None;

        if (!entries.MapDifficulty.IsUsingEncounterLocks() && playerInstanceLock.InstanceId != 0 && playerInstanceLock.InstanceId != instanceLock.InstanceId)
            return TransferAbortReason.LockedToDifferentInstance;

        return TransferAbortReason.None;
    }

    public InstanceLock CreateInstanceLockForNewInstance(ObjectGuid playerGuid, MapDb2Entries entries, uint instanceId)
    {
        if (!entries.MapDifficulty.HasResetSchedule())
            return null;

        InstanceLock instanceLock;

        if (entries.IsInstanceIdBound())
        {
            SharedInstanceLockData sharedData = new(this);
            _instanceLockDataById[instanceId] = sharedData;

            instanceLock = new SharedInstanceLock(entries.MapDifficulty.MapID,
                                                  (Difficulty)entries.MapDifficulty.DifficultyID,
                                                  GetNextResetTime(entries),
                                                  0,
                                                  sharedData,
                                                  this);
        }
        else
            instanceLock = new InstanceLock(entries.MapDifficulty.MapID,
                                            (Difficulty)entries.MapDifficulty.DifficultyID,
                                            GetNextResetTime(entries),
                                            0,
                                            this);

        if (!_temporaryInstanceLocksByPlayer.ContainsKey(playerGuid))
            _temporaryInstanceLocksByPlayer[playerGuid] = new Dictionary<InstanceLockKey, InstanceLock>();

        _temporaryInstanceLocksByPlayer[playerGuid][entries.GetKey()] = instanceLock;

        Log.Logger.Debug($"[{entries.Map.Id}-{entries.Map.MapName[_worldManager.DefaultDbcLocale]} | " +
                         $"{entries.MapDifficulty.DifficultyID}-{_cliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name}] Created new temporary instance lock for {playerGuid} in instance {instanceId}");

        return instanceLock;
    }

    public InstanceLock FindActiveInstanceLock(ObjectGuid playerGuid, MapDb2Entries entries)
    {
        lock (_lockObject)
            return FindActiveInstanceLock(playerGuid, entries, false, true);
    }

    public InstanceLock FindActiveInstanceLock(ObjectGuid playerGuid, MapDb2Entries entries, bool ignoreTemporary, bool ignoreExpired)
    {
        var instanceLock = FindInstanceLock(_instanceLocksByPlayer, playerGuid, entries);

        // Ignore expired and not extended locks
        if (instanceLock != null && (!instanceLock.IsExpired || instanceLock.IsExtended || !ignoreExpired))
            return instanceLock;

        return ignoreTemporary ? null : FindInstanceLock(_temporaryInstanceLocksByPlayer, playerGuid, entries);
    }

    public InstanceLock FindInstanceLock(Dictionary<ObjectGuid, Dictionary<InstanceLockKey, InstanceLock>> locks, ObjectGuid playerGuid, MapDb2Entries entries)
    {
        var playerLocks = locks.LookupByKey(playerGuid);

        return playerLocks?.LookupByKey(entries.GetKey());
    }

    public ICollection<InstanceLock> GetInstanceLocksForPlayer(ObjectGuid playerGuid)
    {
        if (_instanceLocksByPlayer.TryGetValue(playerGuid, out var dictionary))
            return dictionary.Values;

        return new List<InstanceLock>();
    }

    public DateTime GetNextResetTime(MapDb2Entries entries)
    {
        var dateTime = GameTime.DateAndTime;
        var resetHour = _configuration.GetDefaultValue("ResetSchedule:Hour", 8);

        var hour = 0;
        var day = 0;

        switch (entries.MapDifficulty.ResetInterval)
        {
            case MapDifficultyResetInterval.Daily:
            {
                if (dateTime.Hour >= resetHour)
                    day++;

                hour = resetHour;

                break;
            }
            case MapDifficultyResetInterval.Weekly:
            {
                var resetDay = _configuration.GetDefaultValue("ResetSchedule:WeekDay", 2);
                var daysAdjust = resetDay - dateTime.Day;

                if (dateTime.Day > resetDay || (dateTime.Day == resetDay && dateTime.Hour >= resetHour))
                    daysAdjust += 7; // passed it for current week, grab time from next week

                hour = resetHour;
                day += daysAdjust;

                break;
            }
        }

        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day + day, hour, 0, 0);
    }

    /// <summary>
    ///     Retrieves instance lock statistics - for use in GM commands
    /// </summary>
    /// <returns> Statistics info </returns>
    public InstanceLocksStatistics GetStatistics()
    {
        InstanceLocksStatistics statistics;
        statistics.InstanceCount = _instanceLockDataById.Count;
        statistics.PlayerCount = _instanceLocksByPlayer.Count;

        return statistics;
    }

    public void Load()
    {
        Dictionary<uint, SharedInstanceLockData> instanceLockDataById = new();

        //                                              0           1     2
        var result = _characterDatabase.Query("SELECT instanceId, data, completedEncountersMask FROM instance");

        if (!result.IsEmpty())
            do
            {
                var instanceId = result.Read<uint>(0);

                SharedInstanceLockData data = new(this)
                {
                    Data = result.Read<string>(1),
                    CompletedEncountersMask = result.Read<uint>(2),
                    InstanceId = instanceId
                };

                instanceLockDataById[instanceId] = data;
            } while (result.NextRow());

        //                                                  0     1      2       3           4           5     6                        7           8
        var lockResult = _characterDatabase.Query("SELECT guid, mapId, lockId, instanceId, difficulty, data, completedEncountersMask, expiryTime, extended FROM character_instance_lock");

        if (result.IsEmpty())
            return;

        {
            do
            {
                var playerGuid = ObjectGuid.Create(HighGuid.Player, lockResult.Read<ulong>(0));
                var mapId = lockResult.Read<uint>(1);
                var lockId = lockResult.Read<uint>(2);
                var instanceId = lockResult.Read<uint>(3);
                var difficulty = (Difficulty)lockResult.Read<byte>(4);
                var expiryTime = Time.UnixTimeToDateTime(lockResult.Read<long>(7));

                // Mark instance id as being used
                _mapManager.RegisterInstanceId(instanceId);

                InstanceLock instanceLock;

                if (new MapDb2Entries(mapId, difficulty, _cliDB, _db2Manager).IsInstanceIdBound())
                {
                    if (!instanceLockDataById.TryGetValue(instanceId, out var sharedData))
                    {
                        Log.Logger.Error($"Missing instance data for instance id based lock (id {instanceId})");
                        _characterDatabase.Execute($"DELETE FROM character_instance_lock WHERE instanceId = {instanceId}");

                        continue;
                    }

                    instanceLock = new SharedInstanceLock(mapId, difficulty, expiryTime, instanceId, sharedData, this);
                    _instanceLockDataById[instanceId] = sharedData;
                }
                else
                    instanceLock = new InstanceLock(mapId, difficulty, expiryTime, instanceId, this);

                instanceLock.Data.Data = lockResult.Read<string>(5);
                instanceLock.Data.CompletedEncountersMask = lockResult.Read<uint>(6);
                instanceLock.Extended = lockResult.Read<bool>(8);

                _instanceLocksByPlayer[playerGuid][Tuple.Create(mapId, lockId)] = instanceLock;
            } while (result.NextRow());
        }
    }

    public void OnSharedInstanceLockDataDelete(uint instanceId)
    {
        if (_unloading)
            return;

        _instanceLockDataById.Remove(instanceId);
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_INSTANCE);
        stmt.AddValue(0, instanceId);
        _characterDatabase.Execute(stmt);
        Log.Logger.Debug($"Deleting instance {instanceId} as it is no longer referenced by any player");
    }

    /// <summary>
    ///     Resets instances that match given filter - for use in GM commands
    /// </summary>
    /// <param name="playerGuid"> Guid of player whose locks will be removed </param>
    /// <param name="mapId"> (Optional) Map id of instance locks to reset </param>
    /// <param name="difficulty"> (Optional) Difficulty of instance locks to reset </param>
    /// <param name="locksReset"> All locks that were reset </param>
    /// <param name="locksFailedToReset"> Locks that could not be reset because they are used by existing instance map </param>
    public void ResetInstanceLocksForPlayer(ObjectGuid playerGuid, uint? mapId, Difficulty? difficulty, List<InstanceLock> locksReset, List<InstanceLock> locksFailedToReset)
    {
        if (!_instanceLocksByPlayer.TryGetValue(playerGuid, out var playerLocks))
            return;

        foreach (var playerLockPair in playerLocks)
        {
            if (playerLockPair.Value.IsInUse)
            {
                locksFailedToReset.Add(playerLockPair.Value);

                continue;
            }

            if (mapId.HasValue && mapId.Value != playerLockPair.Value.MapId)
                continue;

            if (difficulty.HasValue && difficulty.Value != playerLockPair.Value.DifficultyId)
                continue;

            locksReset.Add(playerLockPair.Value);
        }

        if (locksReset.Empty())
            return;

        SQLTransaction trans = new();

        foreach (var instanceLock in locksReset)
        {
            MapDb2Entries entries = new(instanceLock.MapId, instanceLock.DifficultyId, _cliDB, _db2Manager);
            var newExpiryTime = GetNextResetTime(entries) - TimeSpan.FromSeconds(entries.MapDifficulty.GetRaidDuration());
            // set reset time to last reset time
            instanceLock.ExpiryTime = newExpiryTime;
            instanceLock.Extended = false;

            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHARACTER_INSTANCE_LOCK_FORCE_EXPIRE);
            stmt.AddValue(0, (ulong)Time.DateTimeToUnixTime(newExpiryTime));
            stmt.AddValue(1, playerGuid.Counter);
            stmt.AddValue(2, entries.MapDifficulty.MapID);
            stmt.AddValue(3, entries.MapDifficulty.LockID);
            trans.Append(stmt);
        }

        _characterDatabase.CommitTransaction(trans);
    }

    public void Unload()
    {
        _unloading = true;
        _instanceLocksByPlayer.Clear();
        _instanceLockDataById.Clear();
    }

    public Tuple<DateTime, DateTime> UpdateInstanceLockExtensionForPlayer(ObjectGuid playerGuid, MapDb2Entries entries, bool extended)
    {
        var instanceLock = FindActiveInstanceLock(playerGuid, entries, true, false);

        if (instanceLock == null)
            return Tuple.Create(DateTime.MinValue, DateTime.MinValue);

        var oldExpiryTime = instanceLock.GetEffectiveExpiryTime();
        instanceLock.Extended = extended;
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHARACTER_INSTANCE_LOCK_EXTENSION);
        stmt.AddValue(0, extended ? 1 : 0);
        stmt.AddValue(1, playerGuid.Counter);
        stmt.AddValue(2, entries.MapDifficulty.MapID);
        stmt.AddValue(3, entries.MapDifficulty.LockID);
        _characterDatabase.Execute(stmt);

        Log.Logger.Debug($"[{entries.Map.Id}-{entries.Map.MapName[_worldManager.DefaultDbcLocale]} | " +
                         $"{entries.MapDifficulty.DifficultyID}-{_cliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name}] Instance lock for {playerGuid} is {(extended ? "now" : "no longer")} extended");

        return Tuple.Create(oldExpiryTime, instanceLock.GetEffectiveExpiryTime());
    }

    public InstanceLock UpdateInstanceLockForPlayer(SQLTransaction trans, ObjectGuid playerGuid, MapDb2Entries entries, InstanceLockUpdateEvent updateEvent)
    {
        var instanceLock = FindActiveInstanceLock(playerGuid, entries, true, true);

        if (instanceLock == null)
            lock (_lockObject)
            {
                // Move lock from temporary storage if it exists there
                // This is to avoid destroying expired locks before any boss is killed in a fresh lock
                // player can still change his mind, exit instance and reactivate old lock
                var playerLocks = _temporaryInstanceLocksByPlayer.LookupByKey(playerGuid);

                if (playerLocks?.TryGetValue(entries.GetKey(), out var playerInstanceLock) == true)
                {
                    instanceLock = playerInstanceLock;
                    _instanceLocksByPlayer[playerGuid][entries.GetKey()] = instanceLock;

                    playerLocks.Remove(entries.GetKey());

                    if (playerLocks.Empty())
                        _temporaryInstanceLocksByPlayer.Remove(playerGuid);

                    Log.Logger.Debug($"[{entries.Map.Id}-{entries.Map.MapName[_worldManager.DefaultDbcLocale]} | " +
                                     $"{entries.MapDifficulty.DifficultyID}-{_cliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name}] Promoting temporary lock to permanent for {playerGuid} in instance {updateEvent.InstanceId}");
                }
            }

        if (instanceLock == null)
        {
            if (entries.IsInstanceIdBound())
            {
                var sharedDataItr = _instanceLockDataById.LookupByKey(updateEvent.InstanceId);

                instanceLock = new SharedInstanceLock(entries.MapDifficulty.MapID,
                                                      (Difficulty)entries.MapDifficulty.DifficultyID,
                                                      GetNextResetTime(entries),
                                                      updateEvent.InstanceId,
                                                      sharedDataItr,
                                                      this);
            }
            else
                instanceLock = new InstanceLock(entries.MapDifficulty.MapID,
                                                (Difficulty)entries.MapDifficulty.DifficultyID,
                                                GetNextResetTime(entries),
                                                updateEvent.InstanceId,
                                                this);

            lock (_lockObject)
                _instanceLocksByPlayer[playerGuid][entries.GetKey()] = instanceLock;

            Log.Logger.Debug($"[{entries.Map.Id}-{entries.Map.MapName[_worldManager.DefaultDbcLocale]} | " +
                             $"{entries.MapDifficulty.DifficultyID}-{_cliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name}] Created new instance lock for {playerGuid} in instance {updateEvent.InstanceId}");
        }
        else
            instanceLock.InstanceId = new InClassName(updateEvent.InstanceId);

        instanceLock.Data.Data = updateEvent.NewData;

        if (updateEvent.CompletedEncounter != null)
        {
            instanceLock.Data.CompletedEncountersMask |= 1u << updateEvent.CompletedEncounter.Bit;

            Log.Logger.Debug($"[{entries.Map.Id}-{entries.Map.MapName[_worldManager.DefaultDbcLocale]} | " +
                             $"{entries.MapDifficulty.DifficultyID}-{_cliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name}] " +
                             $"Instance lock for {playerGuid} in instance {updateEvent.InstanceId} gains completed encounter [{updateEvent.CompletedEncounter.Id}-{updateEvent.CompletedEncounter.Name[_worldManager.DefaultDbcLocale]}]");
        }

        // Synchronize map completed encounters into players completed encounters for UI
        if (!entries.MapDifficulty.IsUsingEncounterLocks())
            instanceLock.Data.CompletedEncountersMask |= updateEvent.InstanceCompletedEncountersMask;

        if (updateEvent.EntranceWorldSafeLocId.HasValue)
            instanceLock.Data.EntranceWorldSafeLocId = updateEvent.EntranceWorldSafeLocId.Value;

        if (instanceLock.IsExpired)
        {
            instanceLock.ExpiryTime = GetNextResetTime(entries);
            instanceLock.Extended = false;

            Log.Logger.Debug($"[{entries.Map.Id}-{entries.Map.MapName[_worldManager.DefaultDbcLocale]} | " +
                             $"{entries.MapDifficulty.DifficultyID}-{_cliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name}] Expired instance lock for {playerGuid} in instance {updateEvent.InstanceId} is now active");
        }

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_INSTANCE_LOCK);
        stmt.AddValue(0, playerGuid.Counter);
        stmt.AddValue(1, entries.MapDifficulty.MapID);
        stmt.AddValue(2, entries.MapDifficulty.LockID);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CHARACTER_INSTANCE_LOCK);
        stmt.AddValue(0, playerGuid.Counter);
        stmt.AddValue(1, entries.MapDifficulty.MapID);
        stmt.AddValue(2, entries.MapDifficulty.LockID);
        stmt.AddValue(3, instanceLock.InstanceId);
        stmt.AddValue(4, entries.MapDifficulty.DifficultyID);
        stmt.AddValue(5, instanceLock.Data.Data);
        stmt.AddValue(6, instanceLock.Data.CompletedEncountersMask);
        stmt.AddValue(7, instanceLock.Data.EntranceWorldSafeLocId);
        stmt.AddValue(8, (ulong)Time.DateTimeToUnixTime(instanceLock.ExpiryTime));
        stmt.AddValue(9, instanceLock.IsExtended ? 1 : 0);
        trans.Append(stmt);

        return instanceLock;
    }

    public void UpdateSharedInstanceLock(SQLTransaction trans, InstanceLockUpdateEvent updateEvent)
    {
        var sharedData = _instanceLockDataById.LookupByKey(updateEvent.InstanceId);
        sharedData.Data = updateEvent.NewData;
        sharedData.InstanceId = updateEvent.InstanceId;

        if (updateEvent.CompletedEncounter != null)
        {
            sharedData.CompletedEncountersMask |= 1u << updateEvent.CompletedEncounter.Bit;
            Log.Logger.Debug($"Instance {updateEvent.InstanceId} gains completed encounter [{updateEvent.CompletedEncounter.Id}-{updateEvent.CompletedEncounter.Name[_worldManager.DefaultDbcLocale]}]");
        }

        if (updateEvent.EntranceWorldSafeLocId.HasValue)
            sharedData.EntranceWorldSafeLocId = updateEvent.EntranceWorldSafeLocId.Value;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_INSTANCE);
        stmt.AddValue(0, sharedData.InstanceId);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_INSTANCE);
        stmt.AddValue(0, sharedData.InstanceId);
        stmt.AddValue(1, sharedData.Data);
        stmt.AddValue(2, sharedData.CompletedEncountersMask);
        stmt.AddValue(3, sharedData.EntranceWorldSafeLocId);
        trans.Append(stmt);
    }
}