// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum LoginFailureReason
{
	Failed = 0,
	NoWorld = 1,
	DuplicateCharacter = 2,
	NoInstances = 3,
	Disabled = 4,
	NoCharacter = 5,
	LockedForTransfer = 6,
	LockedByBilling = 7,
	LockedByMobileAH = 8,
	TemporaryGMLock = 9,
	LockedByCharacterUpgrade = 10,
	LockedByRevokedCharacterUpgrade = 11,
	LockedByRevokedVASTransaction = 17,
	LockedByRestriction = 19,
	LockedForRealmPlaytype = 23
}