// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SpellMissInfo
{
	None = 0,
	Miss = 1,
	Resist = 2,
	Dodge = 3,
	Parry = 4,
	Block = 5,
	Evade = 6,
	Immune = 7,
	Immune2 = 8, // One Of These 2 Is MissTempimmune
	Deflect = 9,
	Absorb = 10,
	Reflect = 11
}