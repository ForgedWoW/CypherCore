// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum MapDifficultyFlags : int
{
    LimitToPlayersFromOneRealm = 0x01,
    UseLootBasedLockInsteadOfInstanceLock = 0x02, // Lock to single encounters
    LockedToSoloOwner = 0x04,
    ResumeDungeonProgressBasedOnLockout = 0x08, // Mythic dungeons with this flag zone into leaders instance instead of always using a fresh one (Return to Karazhan, Operation: Mechagon)
    DisableLockExtension = 0x10,
}