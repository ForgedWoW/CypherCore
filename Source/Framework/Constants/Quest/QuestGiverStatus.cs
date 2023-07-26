// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum QuestGiverStatus : ulong
{
    None = 0x000000000,
    Future = 0x000000002,
    Trivial = 0x000000004,
    TrivialRepeatableTurnin = 0x000000008,
    TrivialDailyQuest = 0x000000010,
    Reward = 0x000000020,
    JourneyReward = 0x000000040,
    CovenantCallingReward = 0x000000080,
    RepeatableTurnin = 0x000000100,
    DailyQuest = 0x000000200,
    Quest = 0x000000400,
    RewardCompleteNoPOI = 0x000000800,
    RewardCompletePOI = 0x000001000,
    LegendaryQuest = 0x000002000,
    LegendaryRewardCompleteNoPOI = 0x000004000,
    LegendaryRewardCompletePOI = 0x000008000,
    JourneyQuest = 0x000010000,
    JourneyRewardCompleteNoPOI = 0x000020000,
    JourneyRewardCompletePOI = 0x000040000,
    CovenantCallingQuest = 0x000080000,
    CovenantCallingRewardCompleteNoPOI = 0x000100000,
    CovenantCallingRewardCompletePOI = 0x000200000,
    TrivialLegendaryQuest = 0x000400000,
    FutureLegendaryQuest = 0x000800000,
    LegendaryReward = 0x001000000,
    ImportantQuest = 0x002000000,
    ImportantQuestReward = 0x004000000,
    TrivialImportantQuest = 0x008000000,
    ImportantQuestRewardCompleteNoPOI = 0x020000000,
    ImportantQuestRewardCompletePOI = 0x040000000,
    TrivialJourneyQuest = 0x080000000,
    TrivialCovenantCallingQuest = 0x100000000,
}