// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.AI;

public enum EscortState
{
	None = 0x00,      //nothing in progress
	Escorting = 0x01, //escort are in progress
	Returning = 0x02, //escort is returning after being in combat
	Paused = 0x04     //will not proceed with waypoints before state is removed
}