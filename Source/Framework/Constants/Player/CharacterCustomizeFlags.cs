// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum CharacterCustomizeFlags
{
	None = 0x00,
	Customize = 0x01,  // Name, Gender, Etc...
	Faction = 0x10000, // Name, Gender, Faction, Etc...
	Race = 0x100000    // Name, Gender, Race, Etc...
}