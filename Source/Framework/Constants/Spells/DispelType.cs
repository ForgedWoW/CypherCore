// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum DispelType
{
	None = 0,
	Magic = 1,
	Curse = 2,
	Disease = 3,
	Poison = 4,
	Stealth = 5,
	Invisibility = 6,
	ALL = 7,
	SpeNPCOnly = 8,
	Enrage = 9,
	ZGTicket = 10,
	OldUnused = 11,

	AllMask = ((1 << Magic) | (1 << Curse) | (1 << Disease) | (1 << Poison))
}