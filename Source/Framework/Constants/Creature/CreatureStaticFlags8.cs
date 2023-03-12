// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum CreatureStaticFlags8 : uint
{
	FORCE_CLOSE_IN_ON_PATH_FAIL_BEHAVIOR = 0x00000002,
	USE_2D_CHASING_CALCULATION = 0x00000020,
	USE_FAST_CLASSIC_HEARTBEAT = 0x00000040
}