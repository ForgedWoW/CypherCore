// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum LootError
{
	DidntKill = 0,             // You don't have permission to loot that corpse.
	TooFar = 4,                // You are too far away to loot that corpse.
	BadFacing = 5,             // You must be facing the corpse to loot it.
	Locked = 6,                // Someone is already looting that corpse.
	NotStanding = 8,           // You need to be standing up to loot something!
	Stunned = 9,               // You can't loot anything while stunned!
	PlayerNotFound = 10,       // Player not found
	PlayTimeExceeded = 11,     // Maximum play time exceeded
	MasterInvFull = 12,        // That player's inventory is full
	MasterUniqueItem = 13,     // Player has too many of that item already
	MasterOther = 14,          // Can't assign item to that player
	AlreadPickPocketed = 15,   // Your target has already had its pockets picked
	NotWhileShapeShifted = 16, // You can't do that while shapeshifted.
	NoLoot = 17                // There is no loot.
}