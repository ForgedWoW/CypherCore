// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

/// Type of environmental damages
public enum EnviromentalDamage
{
	Exhausted = 0,
	Drowning = 1,
	Fall = 2,
	Lava = 3,
	Slime = 4,
	Fire = 5,
	FallToVoid = 6 // custom case for fall without durability loss
}