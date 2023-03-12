// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ChannelFlags
{
	None = 0x00,
	Custom = 0x01,

	// 0x02
	Trade = 0x04,
	NotLfg = 0x08,
	General = 0x10,
	City = 0x20,
	Lfg = 0x40,

	Voice = 0x80
	// General                  0x18 = 0x10 | 0x08
	// Trade                    0x3C = 0x20 | 0x10 | 0x08 | 0x04
	// LocalDefence             0x18 = 0x10 | 0x08
	// GuildRecruitment         0x38 = 0x20 | 0x10 | 0x08
	// LookingForGroup          0x50 = 0x40 | 0x10
}