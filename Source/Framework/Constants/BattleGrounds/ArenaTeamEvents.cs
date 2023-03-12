// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ArenaTeamEvents
{
	JoinSs = 3,           // Player Name + Arena Team Name
	LeaveSs = 4,          // Player Name + Arena Team Name
	RemoveSss = 5,        // Player Name + Arena Team Name + Captain Name
	LeaderIsSs = 6,       // Player Name + Arena Team Name
	LeaderChangedSss = 7, // Old Captain + New Captain + Arena Team Name
	DisbandedS = 8        // Captain Name + Arena Team Name
}