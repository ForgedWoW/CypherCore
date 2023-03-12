// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;
// CurrencyTypes.db2 (10.0.5.48317)

public enum AzeriteEssenceActivateResult
{
	None = 0,
	EssenceNotUnlocked = 2, // Arg: AzeriteEssenceID
	CantDoThatRightNow = 3,
	AffectingCombat = 4,
	CantRemoveEssence = 5, // Arg: SpellID of active essence on cooldown
	ChallengeModeActive = 6,
	NotInRestArea = 7,
	ConditionFailed = 8,
	SlotLocked = 9,
	NotAtForge = 10,
	HeartLevelTooLow = 11, // Arg: RequiredLevel
	NotEquipped = 12
}