// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum QuestFailedReasons
{
	None = 0,
	FailedLowLevel = 1,        // "You Are Not High Enough Level For That Quest.""
	FailedWrongRace = 6,       // "That Quest Is Not Available To Your Race."
	AlreadyDone = 7,           // "You Have Completed That Daily Quest Today."
	OnlyOneTimed = 12,         // "You Can Only Be On One Timed Quest At A Time"
	AlreadyOn1 = 13,           // "You Are Already On That Quest"
	FailedExpansion = 16,      // "This Quest Requires An Expansion Enabled Account."
	AlreadyOn2 = 18,           // "You Are Already On That Quest"
	FailedMissingItems = 21,   // "You Don'T Have The Required Items With You.  Check Storage."
	FailedNotEnoughMoney = 23, // "You Don'T Have Enough Money For That Quest"
	FailedCais = 24,           // "You Cannot Complete Quests Once You Have Reached Tired Time"
	AlreadyDoneDaily = 26,     // "You Have Completed That Daily Quest Today."
	FailedSpell = 28,          // "You Haven'T Learned The Required Spell."
	HasInProgress = 30         // "Progress Bar Objective Not Completed"
}