// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum TempSummonType
{
	TimedOrDeadDespawn = 1,      // despawns after a specified time OR when the creature disappears
	TimedOrCorpseDespawn = 2,    // despawns after a specified time OR when the creature dies
	TimedDespawn = 3,            // despawns after a specified time
	TimedDespawnOutOfCombat = 4, // despawns after a specified time after the creature is out of combat
	CorpseDespawn = 5,           // despawns instantly after death
	CorpseTimedDespawn = 6,      // despawns after a specified time after death
	DeadDespawn = 7,             // despawns when the creature disappears
	ManualDespawn = 8            // despawns when UnSummon() is called
}