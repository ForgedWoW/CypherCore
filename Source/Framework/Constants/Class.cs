// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PlayerClass
{
	None = 0,
	Warrior = 1,
	Paladin = 2,
	Hunter = 3,
	Rogue = 4,
	Priest = 5,
	Deathknight = 6,
	Shaman = 7,
	Mage = 8,
	Warlock = 9,
	Monk = 10,
	Druid = 11,
	DemonHunter = 12,
	Evoker = 13,
	Adventurer = 14,
	Max = 15,

	ClassMaskAllPlayable = ((1 << (Warrior - 1)) |
							(1 << (Paladin - 1)) |
							(1 << (Hunter - 1)) |
							(1 << (Rogue - 1)) |
							(1 << (Priest - 1)) |
							(1 << (Deathknight - 1)) |
							(1 << (Shaman - 1)) |
							(1 << (Mage - 1)) |
							(1 << (Warlock - 1)) |
							(1 << (Monk - 1)) |
							(1 << (Druid - 1)) |
							(1 << (DemonHunter - 1)) |
							(1 << (Evoker - 1))),

	ClassMaskAllCreatures = ((1 << (Warrior - 1)) | (1 << (Paladin - 1)) | (1 << (Rogue - 1)) | (1 << (Mage - 1))),

	ClassMaskWandUsers = ((1 << (Priest - 1)) | (1 << (Mage - 1)) | (1 << (Warlock - 1)))
}