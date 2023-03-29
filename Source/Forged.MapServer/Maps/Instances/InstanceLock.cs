// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Chrono;
using Framework.Constants;

namespace Forged.MapServer.Maps.Instances;

public class InstanceLock
{
    private readonly uint _mapId;
    private readonly Difficulty _difficultyId;
    private readonly InstanceLockData _data = new();
    private uint _instanceId;
    private DateTime _expiryTime;
    private bool _extended;
    private bool _isInUse;

    public InstanceLock(uint mapId, Difficulty difficultyId, DateTime expiryTime, uint instanceId)
    {
        _mapId = mapId;
        _difficultyId = difficultyId;
        _instanceId = instanceId;
        _expiryTime = expiryTime;
        _extended = false;
    }

    public bool IsExpired()
    {
        return _expiryTime < GameTime.GetSystemTime();
    }

    public DateTime GetEffectiveExpiryTime()
    {
        if (!IsExtended())
            return GetExpiryTime();

        MapDb2Entries entries = new(_mapId, _difficultyId);

        // return next reset time
        if (IsExpired())
            return Global.InstanceLockMgr.GetNextResetTime(entries);

        // if not expired, return expiration time + 1 reset period
        return GetExpiryTime() + TimeSpan.FromSeconds(entries.MapDifficulty.GetRaidDuration());
    }

    public uint GetMapId()
    {
        return _mapId;
    }

    public Difficulty GetDifficultyId()
    {
        return _difficultyId;
    }

    public uint GetInstanceId()
    {
        return _instanceId;
    }

    public void SetInstanceId(uint instanceId)
    {
        _instanceId = instanceId;
    }

    public DateTime GetExpiryTime()
    {
        return _expiryTime;
    }

    public void SetExpiryTime(DateTime expiryTime)
    {
        _expiryTime = expiryTime;
    }

    public bool IsExtended()
    {
        return _extended;
    }

    public void SetExtended(bool extended)
    {
        _extended = extended;
    }

    public InstanceLockData GetData()
    {
        return _data;
    }

    public virtual InstanceLockData GetInstanceInitializationData()
    {
        return _data;
    }

    public bool IsInUse()
    {
        return _isInUse;
    }

    public void SetInUse(bool inUse)
    {
        _isInUse = inUse;
    }
}