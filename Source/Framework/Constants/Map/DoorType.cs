// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum DoorType
{
	Room = 0,      // Door can open if encounter is not in progress
	Passage = 1,   // Door can open if encounter is done
	SpawnHole = 2, // Door can open if encounter is in progress, typically used for spawning places
	Max
}