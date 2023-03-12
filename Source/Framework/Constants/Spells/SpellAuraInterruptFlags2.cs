// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellAuraInterruptFlags2
{
	None = 0,
	Falling = 0x01, // NYI
	Swimming = 0x02,
	NotMoving = 0x04, // NYI
	Ground = 0x08,
	Transform = 0x10, // NYI
	Jump = 0x20,
	ChangeSpec = 0x40,
	AbandonVehicle = 0x80,             // NYI
	StartOfEncounter = 0x100,          // NYI
	EndOfEncounter = 0x200,            // NYI
	Disconnect = 0x400,                // NYI
	EnteringInstance = 0x800,          // NYI
	DuelEnd = 0x1000,                  // NYI
	LeaveArenaOrBattleground = 0x2000, // NYI
	ChangeTalent = 0x4000,
	ChangeGlyph = 0x8000,
	SeamlessTransfer = 0x10000,          // NYI
	WarModeLeave = 0x20000,              // NYI
	TouchingGround = 0x40000,            // NYI
	ChromieTime = 0x80000,               // NYI
	SplineFlightOrFreeFlight = 0x100000, // NYI
	ProcOrPeriodicAttacking = 0x200000   // NYI
}