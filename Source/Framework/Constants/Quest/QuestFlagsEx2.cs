// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum QuestFlagsEx2
{
    ResetOnGameMilestone = 0x01,
    NoWarModeBonus = 0x02,
    AwardHighestProfession = 0x04,
    NotReplayable = 0x08,
    NoReplayRewards = 0x10,
    DisableWaypointPathing = 0x20,
    ResetOnMythicPlusSeason = 0x40,
    ResetOnPvpSeason = 0x80,
    EnableOverrideSortOrder = 0x100,
    ForceStartingLocOnZoneMap = 0x200,
    BonusLootNever = 0x400,
    BonusLootAlways = 0x800,
    HideTaskOnMainMap = 0x1000,
    HideTaskInTracker = 0x2000,
    SkipDisabledCheck = 0x4000,
    EnforceMaximumQuestLevel = 0x8000
}