// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum LfgCompatibility
{
	Pending,
	WrongGroupSize,
	TooMuchPlayers,
	MultipleLfgGroups,
	HasIgnores,
	NoRoles,
	NoDungeons,
	WithLessPlayers, // Values Under This = Not Compatible (Do Not Modify Order)
	BadStates,
	Match // Must Be The Last One
}