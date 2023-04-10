// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgLockInfoData
{
    public float CurrentItemLevel { get; set; }
    public LfgLockStatusType LockStatus { get; set; }
    public ushort RequiredItemLevel { get; set; }

    public LfgLockInfoData(LfgLockStatusType lockStatus = 0, ushort requiredItemLevel = 0, float currentItemLevel = 0)
    {
        LockStatus = lockStatus;
        RequiredItemLevel = requiredItemLevel;
        CurrentItemLevel = currentItemLevel;
    }
}