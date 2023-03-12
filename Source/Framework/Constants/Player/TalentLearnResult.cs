// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum TalentLearnResult
{
	LearnOk = 0,
	FailedUnknown = 1,
	FailedNotEnoughTalentsInPrimaryTree = 2,
	FailedNoPrimaryTreeSelected = 3,
	FailedCantDoThatRightNow = 4,
	FailedAffectingCombat = 5,
	FailedCantRemoveTalent = 6,
	FailedCantDoThatChallengeModeActive = 7,
	FailedRestArea = 8,
	UnspentTalentPoints = 9,
	InPvpMatch = 10
}