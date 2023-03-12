// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PartyResult
{
	Ok = 0,
	BadPlayerNameS = 1,
	TargetNotInGroupS = 2,
	TargetNotInInstanceS = 3,
	GroupFull = 4,
	AlreadyInGroupS = 5,
	NotInGroup = 6,
	NotLeader = 7,
	PlayerWrongFaction = 8,
	IgnoringYouS = 9,
	LfgPending = 12,
	InviteRestricted = 13,
	GroupSwapFailed = 14, // If (Partyoperation == PartyOpSwap) GroupSwapFailed Else InviteInCombat
	InviteUnknownRealm = 15,
	InviteNoPartyServer = 16,
	InvitePartyBusy = 17,
	PartyTargetAmbiguous = 18,
	PartyLfgInviteRaidLocked = 19,
	PartyLfgBootLimit = 20,
	PartyLfgBootCooldownS = 21,
	PartyLfgBootInProgress = 22,
	PartyLfgBootTooFewPlayers = 23,
	PartyLfgBootNotEligibleS = 24,
	RaidDisallowedByLevel = 25,
	PartyLfgBootInCombat = 26,
	VoteKickReasonNeeded = 27,
	PartyLfgBootDungeonComplete = 28,
	PartyLfgBootLootRolls = 29,
	PartyLfgTeleportInCombat = 30
}