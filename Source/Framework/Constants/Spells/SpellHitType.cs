// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellHitType
{
	CritDebu = 0x1,
	Crit = 0x2,
	HitDebug = 0x4,
	Split = 0x8,
	VictimIsAttacker = 0x10,
	AttackTableDebug = 0x20,
	Unk = 0x40,
	NoAttacker = 0x80 // does the same as SPELL_ATTR4_COMBAT_LOG_NO_CASTER
}