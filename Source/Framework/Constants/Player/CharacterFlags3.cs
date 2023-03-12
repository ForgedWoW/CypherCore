// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum CharacterFlags3 : uint
{
	LockedByRevokedVasTransaction = 0x100000,
	LockedByRevokedCharacterUpgrade = 0x80000000,
}