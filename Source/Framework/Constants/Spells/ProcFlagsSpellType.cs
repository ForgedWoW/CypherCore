// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum ProcFlagsSpellType
{
	None = 0x0,
	Damage = 0x1,    // damage type of spell
	Heal = 0x2,      // heal type of spell
	NoDmgHeal = 0x4, // other spells
	MaskAll = Damage | Heal | NoDmgHeal
}