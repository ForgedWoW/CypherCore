// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellSchoolMask
{
	None = 0x0,                          // Not Exist
	Normal = (1 << SpellSchools.Normal), // Physical (Armor)
	Holy = (1 << SpellSchools.Holy),
	Fire = (1 << SpellSchools.Fire),
	Nature = (1 << SpellSchools.Nature),
	Frost = (1 << SpellSchools.Frost),
	Shadow = (1 << SpellSchools.Shadow),
	Arcane = (1 << SpellSchools.Arcane),

	// 124, Not Include Normal And Holy Damage
	Spell = (Fire | Nature | Frost | Shadow | Arcane),

	// 126
	Magic = (Holy | Spell),

	// 127
	All = (Normal | Magic),
}