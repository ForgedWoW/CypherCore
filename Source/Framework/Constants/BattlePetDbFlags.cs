// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum BattlePetDbFlags : ushort
{
	None = 0x00,
	Favorite = 0x01,
	Converted = 0x02,
	Revoked = 0x04,
	LockedForConvert = 0x08,
	Ability0Selection = 0x10,
	Ability1Selection = 0x20,
	Ability2Selection = 0x40,
	FanfareNeeded = 0x80,
	DisplayOverridden = 0x100
}