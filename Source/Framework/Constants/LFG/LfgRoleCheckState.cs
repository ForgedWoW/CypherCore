// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum LfgRoleCheckState
{
	Default = 0,      // Internal Use = Not Initialized.
	Finished = 1,     // Role Check Finished
	Initialiting = 2, // Role Check Begins
	MissingRole = 3,  // Someone Didn'T Selected A Role After 2 Mins
	WrongRoles = 4,   // Can'T Form A Group With That Role Selection
	Aborted = 5,      // Someone Leave The Group
	NoRole = 6        // Someone Selected No Role
}