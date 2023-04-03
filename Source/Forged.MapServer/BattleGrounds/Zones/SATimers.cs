// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.BattleGrounds.Zones;

internal struct SaTimers
{
    public const uint BOAT_START = 60 * Time.IN_MILLISECONDS;
    public const uint ROUND_LENGTH = 600 * Time.IN_MILLISECONDS;
    public const uint WARMUP_LENGTH = 120 * Time.IN_MILLISECONDS;
}