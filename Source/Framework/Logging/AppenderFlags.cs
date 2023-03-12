// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

[Flags]
enum AppenderFlags
{
	None = 0x00,
	PrefixTimestamp = 0x01,
	PrefixLogLevel = 0x02,
	PrefixLogFilterType = 0x04,
}