// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Maps.Instances;

internal class SharedInstanceLockData : InstanceLockData
{
    private readonly InstanceLockManager _instanceLockManager;

    public SharedInstanceLockData(InstanceLockManager instanceLockManager)
    {
        _instanceLockManager = instanceLockManager;
    }

    ~SharedInstanceLockData()
    {
        // Cleanup database
        if (InstanceId != 0)
            _instanceLockManager.OnSharedInstanceLockDataDelete(InstanceId);
    }

    public uint InstanceId { get; set; }
}