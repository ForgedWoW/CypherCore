// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum FactionMasks : byte
{
	Player = 1,   // any player
	Alliance = 2, // player or creature from alliance team
	Horde = 4,    // player or creature from horde team

	Monster = 8 // aggressive creature from monster team
	// if none flags set then non-aggressive creature
}