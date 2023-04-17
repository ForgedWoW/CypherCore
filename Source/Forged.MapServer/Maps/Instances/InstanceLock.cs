// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Chrono;
using Framework.Constants;

namespace Forged.MapServer.Maps.Instances;

public class InstanceLock
{
    private readonly InstanceLockData _data = new();
    private readonly Difficulty _difficultyId;
    private readonly InstanceLockManager _instanceLockManager;
    private readonly uint _mapId;
    private DateTime _expiryTime;
    private bool _extended;
    private uint _instanceId;
    private bool _isInUse;

    public InstanceLock(uint mapId, Difficulty difficultyId, DateTime expiryTime, uint instanceId, InstanceLockManager instanceLockManager)
    {
        _mapId = mapId;
        _difficultyId = difficultyId;
        _instanceId = instanceId;
        _instanceLockManager = instanceLockManager;
        _expiryTime = expiryTime;
        _extended = false;
    }

    public InstanceLockData GetData()
    {
        return _data;
    }

    public Difficulty GetDifficultyId()
    {
        return _difficultyId;
    }

    public DateTime GetEffectiveExpiryTime()
    {
        if (!IsExtended())
            return GetExpiryTime();

        MapDb2Entries entries = new(_mapId, _difficultyId);

        // return next reset time
        if (IsExpired())
            return _instanceLockManager.GetNextResetTime(entries);

        // if not expired, return expiration time + 1 reset period
        return GetExpiryTime() + TimeSpan.FromSeconds(entries.MapDifficulty.GetRaidDuration());
    }

    public DateTime GetExpiryTime()
    {
        return _expiryTime;
    }

    public uint GetInstanceId()
    {
        return _instanceId;
    }

    public virtual InstanceLockData GetInstanceInitializationData()
    {
        return _data;
    }

    public uint GetMapId()
    {
        return _mapId;
    }

    public bool IsExpired()
    {
        return _expiryTime < GameTime.SystemTime;
    }

    public bool IsExtended()
    {
        return _extended;
    }

    public bool IsInUse()
    {
        return _isInUse;
    }

    public void SetExpiryTime(DateTime expiryTime)
    {
        _expiryTime = expiryTime;
    }

    public void SetExtended(bool extended)
    {
        _extended = extended;
    }

    public void SetInstanceId(uint instanceId)
    {
        _instanceId = instanceId;
    }

    public void SetInUse(bool inUse)
    {
        _isInUse = inUse;
    }
}