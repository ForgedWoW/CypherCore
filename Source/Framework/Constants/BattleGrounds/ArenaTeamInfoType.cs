// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ArenaTeamInfoType
{
	Id = 0,
	Type = 1,   // new in 3.2 - team type?
	Member = 2, // 0 - captain, 1 - member
	GamesWeek = 3,
	GamesSeason = 4,
	WinsSeason = 5,
	PersonalRating = 6,
	End = 7
}