// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SkillLineFlags
{
	AlwaysShownInUI = 0x01,
	NeverShownInUI = 0x02,
	FirstTierIsSelfTaught = 0x04,
	GrantedIncrementallyByCharacterUpgrade = 0x08,
	AutomaticRank = 0x0010,
	InheritParentRankWhenLearned = 0x20,
	ShowsInSpellTooltip = 0x40,
	AppearsInMiscTabOfSpellbook = 0x80,

	// unused                                       = 0x0100,
	IgnoreCategoryMods = 0x200,
	DisplaysAsProficiency = 0x400,
	PetsOnly = 0x0800,
	UniqueBitfield = 0x1000,
	RacialForThePurposeOfPaidRaceOrFactionChange = 0x2000,
	ProgressiveSkillUp = 0x4000,
	RacialForThePurposeOfTemporaryRaceChange = 0x8000,
}