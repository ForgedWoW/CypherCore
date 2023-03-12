// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum GroupJoinBattlegroundResult
{
	None = 0,
	Deserters = 2,                   // You Cannot Join The BattlegroundYet Because You Or One Of Your Party Members Is Flagged As A Deserter.
	ArenaTeamPartySize = 3,          // Incorrect Party Size For This Arena.
	TooManyQueues = 4,               // You Can Only Be Queued For 2 Battles At Once
	CannotQueueForRated = 5,         // You Cannot Queue For A Rated Match While Queued For Other Battles
	BattledgroundQueuedForRated = 6, // You Cannot Queue For Another Battle While Queued For A Rated Arena Match
	TeamLeftQueue = 7,               // Your Team Has Left The Arena Queue
	NotInBattleground = 8,           // You Can'T Do That In A Battleground.
	JoinXpGain = 9,                  // Cannot join as a group unless all the members of your party have the same XP gain setting.
	JoinRangeIndex = 10,             // Cannot Join The Queue Unless All Members Of Your Party Are In The Same BattlegroundLevel Range.
	JoinTimedOut = 11,               // %S Was Unavailable To Join The Queue. (Uint64 Guid Exist In Client Cache)

	//JoinTimedOut               = 12,       // Same As 11
	//TeamLeftQueue              = 13,       // Same As 7
	LfgCantUseBattleground = 14,                 // You Cannot Queue For A BattlegroundOr Arena While Using The Dungeon System.
	InRandomBg = 15,                             // Can'T Do That While In A Random BattlegroundQueue.
	InNonRandomBg = 16,                          // Can'T Queue For Random BattlegroundWhile In Another BattlegroundQueue.
	BgDeveloperOnly = 17,                        // This Battleground Is Only Available For Developer Testing At This Time.
	BattlegroundInvitationDeclined = 18,         // Your War Game Invitation Has Been Declined
	MeetingStoneNotFound = 19,                   // Player Not Found.
	WargameRequestFailure = 20,                  // War Game Request Failed
	BattlefieldTeamPartySize = 22,               // Incorrect Party Size For This Battlefield.
	NotOnTournamentRealm = 23,                   // Not Available On A Tournament Realm.
	BattlegroundPlayersFromDifferentRealms = 24, // You Cannot Queue For A Battleground While Players From Different Realms Are In Your Party.
	BattlegroundJoinLevelup = 33,                // You Have Been Removed From A Pvp Queue Because You Have Gained A Level.
	RemoveFromPvpQueueFactionChange = 34,        // You Have Been Removed From A Pvp Queue Because You Changed Your Faction.
	BattlegroundJoinFailed = 35,                 // Join As A Group Failed
	BattlegroundDupeQueue = 43,                  // Someone In Your Group Is Already Queued For That.
	BattlegroundJoinNoValidSpecForRole = 44,     // Role Check Failed Because One Of Your Party Members Selected An Invalid Role.
	BattlegroundJoinRespec = 45,                 // You Have Been Removed From A Pvp Queue Because Your Specialization Changed.
	AlreadyUsingLfgList = 46,                    // You Can'T Do That While Using Premade Groups.
	BattlegroundJoinMustCompleteQuest = 47,      // You Have Been Removed From A Pvp Queue Because Someone Is Missing Required Quest Completion.
	BattlergoundRestrictedAccount = 48,          // Free Trial Accounts Cannot Perform That Action
	BattlegroundJoinMercenary = 49,              // Cannot Join As A Group Unless All The Members Of Your Party Are Flagged As A Mercenary.
	BattlegroundJoinTooManyHealers = 51,         // You Can Not Enter This Bracket Of Arena With More Than One Healer. / You Can Not Enter A Rated Battleground With More Than Three Healers.
	BattlegroundJoinTooManyTanks = 52,           // You Can Not Enter This Bracket Of Arena With More Than One Tank.
	BattlegroundJoinTooManyDamage = 53,          // You Can Not Enter This Bracket Of Arena With More Than Two Damage Dealers.
	GroupJoinBattlegroundDead = 57,              // You Cannot Join The Battleground Because You Or One Of Your Party Members Is Dead.
	BattlegroundJoinRequiresLevel = 58,          // Tournament Rules Requires All Participants To Be Max Level.
	BattlegroundJoinDisqualified = 59,           // %S Has Been Disqualified From Ranked Play In This Bracket.
	ArenaExpiredCais = 60,                       // You May Not Queue While One Or More Of Your Team Members Is Under The Effect Of Restricted Play.
	SoloShuffleWargameGroupSize = 64,            // Exactly 6 Non-Spectator Players Must Be Present To Begin A Solo Shuffle Wargame.
	SoloShuffleWargameGroupComp = 65,            // Exactly 4 Dps, And Either 2 Tanks Or 2 Healers, Must Be Present To Begin A Solo Shuffle Wargame.
}