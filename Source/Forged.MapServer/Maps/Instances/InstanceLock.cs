// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Framework.Constants;

namespace Forged.MapServer.Maps.Instances;

public class InstanceLock
{
    private readonly CliDB _cliDB;
    private readonly DB2Manager _db2Manager;
    private readonly InstanceLockManager _instanceLockManager;

    public InstanceLock(uint mapId, Difficulty difficultyId, DateTime expiryTime, uint instanceId, InstanceLockManager instanceLockManager, CliDB cliDB, DB2Manager db2Manager)
    {
        _instanceLockManager = instanceLockManager;
        _cliDB = cliDB;
        _db2Manager = db2Manager;
        MapId = mapId;
        DifficultyId = difficultyId;
        InstanceId = instanceId;
        ExpiryTime = expiryTime;
        IsExtended = false;
    }

    public InstanceLockData Data { get; } = new();

    public Difficulty DifficultyId { get; }

    public DateTime ExpiryTime { get; set; }

    public uint InstanceId { get; set; }

    public virtual InstanceLockData InstanceInitializationData => Data;

    public bool IsExpired => ExpiryTime < GameTime.SystemTime;

    public bool IsExtended { get; set; }

    public bool IsInUse { get; set; }

    public uint MapId { get; }

    public DateTime GetEffectiveExpiryTime()
    {
        if (!IsExtended)
            return ExpiryTime;

        MapDb2Entries entries = new(MapId, DifficultyId, _cliDB, _db2Manager);

        // return next reset time
        if (IsExpired)
            return _instanceLockManager.GetNextResetTime(entries);

        // if not expired, return expiration time + 1 reset period
        return ExpiryTime + TimeSpan.FromSeconds(entries.MapDifficulty.GetRaidDuration());
    }
}