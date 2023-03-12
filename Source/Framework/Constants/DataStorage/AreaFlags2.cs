// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum AreaFlags2
{
	DontShowSanctuary = 0x200, // Hides sanctuary status from zone text color (Script_GetZonePVPInfo)
	CanEnableWarMode = 0x1000, // Allows enabling war mode
}