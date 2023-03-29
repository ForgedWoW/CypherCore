// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Maps.Instances;

internal class SharedInstanceLockData : InstanceLockData
{
    public uint InstanceId;

    ~SharedInstanceLockData()
    {
        // Cleanup database
        if (InstanceId != 0)
            Global.InstanceLockMgr.OnSharedInstanceLockDataDelete(InstanceId);
    }
}