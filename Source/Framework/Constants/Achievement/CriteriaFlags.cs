// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum CriteriaFlags
{
	FailAchievement = 0x01,        // Fail Achievement
	ResetOnStart = 0x02,           // Reset on Start
	ServerOnly = 0x04,             // Server Only
	AlwaysSaveToDB = 0x08,         // Always Save to DB (Use with Caution)
	AllowCriteriaDecrement = 0x10, // Allow criteria to be decremented
	IsForQuest = 0x20              // Is For Quest
}