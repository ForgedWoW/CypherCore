// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum GuildMemberFlags
{
	None = 0x00,
	Online = 0x01,
	AFK = 0x02,
	DND = 0x04,
	Mobile = 0x08
}