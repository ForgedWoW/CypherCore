// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum CriteriaDataType
{
	None = 0,
	TCreature = 1,
	TPlayerClassRace = 2,
	TPlayerLessHealth = 3,
	SAura = 5,
	TAura = 7,
	Value = 8,
	TLevel = 9,
	TGender = 10,
	Script = 11,

	// Reuse
	MapPlayerCount = 13,
	TTeam = 14,
	SDrunk = 15,
	Holiday = 16,
	BgLossTeamScore = 17,
	InstanceScript = 18,
	SEquippedItem = 19,
	MapId = 20,
	SPlayerClassRace = 21,

	// Reuse
	SKnownTitle = 23,
	GameEvent = 24,
	SItemQuality = 25,

	Max
}