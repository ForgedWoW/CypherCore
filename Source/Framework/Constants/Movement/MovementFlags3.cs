// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum MovementFlags3
{
	None = 0x00,
	DisableInertia = 0x01,
	CanAdvFly = 0x02,
	AdvFlying = 0x04,
}