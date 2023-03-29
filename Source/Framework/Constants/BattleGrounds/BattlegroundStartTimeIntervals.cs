// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum BattlegroundStartTimeIntervals
{
    Delay2m = 120000, // Ms (2 Minutes)
    Delay1m = 60000,  // Ms (1 Minute)
    Delay30s = 30000, // Ms (30 Seconds)
    Delay15s = 15000, // Ms (15 Seconds) Used Only In Arena
    None = 0          // Ms
}