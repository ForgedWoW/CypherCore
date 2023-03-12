// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum QuestFlags : uint
{
	None = 0x00,
	StayAlive = 0x01,            // Not Used Currently
	PartyAccept = 0x02,          // Not Used Currently. If Player In Party, All Players That Can Accept This Quest Will Receive Confirmation Box To Accept Quest Cmsg_Quest_Confirm_Accept/Smsg_Quest_Confirm_Accept
	Exploration = 0x04,          // Not Used Currently
	Sharable = 0x08,             // Can Be Shared: Player.Cansharequest()
	HasCondition = 0x10,         // Not Used Currently
	HideRewardPoi = 0x20,        // Not Used Currently: Unsure Of Content
	Raid = 0x40,                 // Can be completed while in raid
	WarModeRewardsOptIn = 0x80,  // Not Used Currently
	NoMoneyFromXp = 0x100,       // Not Used Currently: Experience Is Not Converted To Gold At Max Level
	HiddenRewards = 0x200,       // Items And Money Rewarded Only Sent In Smsg_Questgiver_Offer_Reward (Not In Smsg_Questgiver_Quest_Details Or In Client Quest Log(Smsg_Quest_Query_Response))
	Tracking = 0x400,            // These Quests Are Automatically Rewarded On Quest Complete And They Will Never Appear In Quest Log Client Side.
	DeprecateReputation = 0x800, // Not Used Currently
	Daily = 0x1000,              // Used To Know Quest Is Daily One
	Pvp = 0x2000,                // Having This Quest In Log Forces Pvp Flag
	Unavailable = 0x4000,        // Used On Quests That Are Not Generically Available
	Weekly = 0x8000,
	AutoComplete = 0x10000,         // Quests with this flag player submit automatically by special button in player gui
	DisplayItemInTracker = 0x20000, // Displays Usable Item In Quest Tracker
	ObjText = 0x40000,              // Use Objective Text As Complete Text
	AutoAccept = 0x80000,           // The client recognizes this flag as auto-accept.
	PlayerCastOnAccept = 0x100000,
	PlayerCastOnComplete = 0x200000,
	UpdatePhaseShift = 0x400000,
	SorWhitelist = 0x800000,
	LaunchGossipComplete = 0x1000000,
	RemoveExtraGetItems = 0x2000000,
	HideUntilDiscovered = 0x4000000,
	PortraitInQuestLog = 0x8000000,
	ShowItemWhenCompleted = 0x10000000,
	LaunchGossipAccept = 0x20000000,
	ItemsGlowWhenDone = 0x40000000,
	FailOnLogout = 0x80000000
}