// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SmartEventFlags
{
	NotRepeatable = 0x01, //Event can not repeat
	Difficulty0 = 0x02,   //Event only occurs in instance difficulty 0
	Difficulty1 = 0x04,   //Event only occurs in instance difficulty 1
	Difficulty2 = 0x08,   //Event only occurs in instance difficulty 2
	Difficulty3 = 0x10,   //Event only occurs in instance difficulty 3
	Reserved5 = 0x20,
	Reserved6 = 0x40,
	DebugOnly = 0x80,     //Event only occurs in debug build
	DontReset = 0x100,    //Event will not reset in SmartScript.OnReset()
	WhileCharmed = 0x200, //Event occurs even if AI owner is charmed

	DifficultyAll = (Difficulty0 | Difficulty1 | Difficulty2 | Difficulty3),
	All = (NotRepeatable | DifficultyAll | Reserved5 | Reserved6 | DebugOnly | DontReset | WhileCharmed),

	// Temp flags, used only at runtime, never stored in DB
	TempIgnoreChanceRoll = 0x40000000, //Event occurs no matter what roll_chance_i(e.event.event_chance) returns.
}