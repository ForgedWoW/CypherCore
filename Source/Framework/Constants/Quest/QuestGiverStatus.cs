// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum QuestGiverStatus : ulong
{
    None = 0x00,
    Future = 0x02,
    Trivial = 0x04,
    TrivialRepeatableTurnin = 0x08,
    TrivialDailyQuest = 0x10,
    Reward = 0x20,
    JourneyReward = 0x40,
    CovenantCallingReward = 0x80,
    RepeatableTurnin = 0x100,
    DailyQuest = 0x200,
    Quest = 0x400,
    RewardCompleteNoPOI = 0x800,
    RewardCompletePOI = 0x1000,
    LegendaryQuest = 0x2000,
    LegendaryRewardCompleteNoPOI = 0x4000,
    LegendaryRewardCompletePOI = 0x8000,
    JourneyQuest = 0x10000,
    JourneyRewardCompleteNoPOI = 0x20000,
    JourneyRewardCompletePOI = 0x40000,
    CovenantCallingQuest = 0x80000,
    CovenantCallingRewardCompleteNoPOI = 0x100000,
    CovenantCallingRewardCompletePOI = 0x200000,
}