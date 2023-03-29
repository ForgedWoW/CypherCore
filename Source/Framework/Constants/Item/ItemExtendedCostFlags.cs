// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ItemExtendedCostFlags
{
    RequireGuild = 0x01,
    RequireSeasonEarned1 = 0x02,
    RequireSeasonEarned2 = 0x04,
    RequireSeasonEarned3 = 0x08,
    RequireSeasonEarned4 = 0x10,
    RequireSeasonEarned5 = 0x20,
}