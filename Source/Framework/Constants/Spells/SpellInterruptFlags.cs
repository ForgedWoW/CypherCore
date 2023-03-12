// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellInterruptFlags
{
	None = 0,
	Movement = 0x01,
	DamagePushbackPlayerOnly = 0x02,
	Stun = 0x04, // useless, even spells without it get interrupted
	Combat = 0x08,
	DamageCancelsPlayerOnly = 0x10,
	MeleeCombat = 0x20, // NYI
	Immunity = 0x40,    // NYI
	DamageAbsorb = 0x80,
	ZeroDamageCancels = 0x100,
	DamagePushback = 0x200,
	DamageCancels = 0x400
}