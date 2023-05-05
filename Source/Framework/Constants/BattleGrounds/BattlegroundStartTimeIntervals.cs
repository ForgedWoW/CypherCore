// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum BattlegroundStartTimeIntervals
{
    Delay2M = 120000, // Ms (2 Minutes)
    Delay1M = 60000,  // Ms (1 Minute)
    Delay30S = 30000, // Ms (30 Seconds)
    Delay15S = 15000, // Ms (15 Seconds) Used Only In Arena
    None = 0          // Ms
}