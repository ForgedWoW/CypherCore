// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum QuestFlagsEx : uint
{
	None = 0x00,
	KeepAdditionalItems = 0x01,
	SuppressGossipComplete = 0x02,
	SuppressGossipAccept = 0x04,
	DisallowPlayerAsQuestgiver = 0x08,
	DisplayClassChoiceRewards = 0x10,
	DisplaySpecChoiceRewards = 0x20,
	RemoveFromLogOnPeriodicReset = 0x40,
	AccountLevelQuest = 0x80,
	LegendaryQuest = 0x100,
	NoGuildXp = 0x200,
	ResetCacheOnAccept = 0x400,
	NoAbandonOnceAnyObjectiveComplete = 0x800,
	RecastAcceptSpellOnLogin = 0x1000,
	UpdateZoneAuras = 0x2000,
	NoCreditForProxy = 0x4000,
	DisplayAsDailyQuest = 0x8000,
	PartOfQuestLine = 0x10000,
	QuestForInternalBuildsOnly = 0x20000,
	SuppressSpellLearnTextLine = 0x40000,
	DisplayHeaderAsObjectiveForTasks = 0x80000,
	GarrisonNonOwnersAllowed = 0x100000,
	RemoveQuestOnWeeklyReset = 0x200000,
	SuppressFarewellAudioAfterQuestAccept = 0x400000,
	RewardsBypassWeeklyCapsAndSeasonTotal = 0x800000,
	IsWorldQuest = 0x1000000,
	NotIgnorable = 0x2000000,
	AutoPush = 0x4000000,
	NoSpellCompleteEffects = 0x8000000,
	DoNotToastHonorReward = 0x10000000,
	KeepRepeatableQuestOnFactionChange = 0x20000000,
	KeepProgressOnFactionChange = 0x40000000,
	PushTeamQuestUsingMapController = 0x80000000
}