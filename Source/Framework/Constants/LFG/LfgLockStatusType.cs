// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum LfgLockStatusType
{
    InsufficientExpansion = 1,
    TooLowLevel = 2,
    TooHighLevel = 3,
    TooLowGearScore = 4,
    TooHighGearScore = 5,
    RaidLocked = 6,
    NoSpec = 14,
    HasRestriction = 15,
    AttunementTooLowLevel = 1001,
    AttunementTooHighLevel = 1002,
    QuestNotCompleted = 1022,
    MissingItem = 1025,
    NotInSeason = 1031,
    MissingAchievement = 1034
}