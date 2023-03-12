// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum PhaseUseFlagsValues : byte
{
	None = 0x0,
	AlwaysVisible = 0x1,
	Inverse = 0x2,

	All = AlwaysVisible | Inverse
}