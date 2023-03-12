// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum PlayerExtraFlags
{
	// gm abilities
	GMOn = 0x01,
	AcceptWhispers = 0x04,
	TaxiCheat = 0x08,
	GMInvisible = 0x10,
	GMChat = 0x20, // Show GM badge in chat messages

	// other states
	PVPDeath = 0x100, // store PvP death status until corpse creating.

	// Character services markers
	HasRaceChanged = 0x0200,
	GrantedLevelsFromRaf = 0x0400,
	LevelBoosted = 0x0800
}