// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Game;

[Flags]
public enum PhaseFlags : ushort
{
	None = 0x0,
	Cosmetic = 0x1,
	Personal = 0x2
}