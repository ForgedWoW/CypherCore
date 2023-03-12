// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SkillRaceClassInfoFlags : ushort
{
	NoSkillupMessage = 0x2,
	AlwaysMaxValue = 0x10,
	Unlearnable = 0x20,   // Skill can be unlearned
	IncludeInSort = 0x80, // Spells belonging to a skill with this flag will additionally compare skill ids when sorting spellbook in client
	NotTrainable = 0x100,
	MonoValue = 0x400 // Skill always has value 1
}