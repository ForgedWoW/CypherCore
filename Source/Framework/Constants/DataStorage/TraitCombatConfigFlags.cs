// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum TraitCombatConfigFlags
{
	None = 0x0,
	ActiveForSpec = 0x1,
	StarterBuild = 0x2,
	SharedActionBars = 0x4
}