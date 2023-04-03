// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.BattleGrounds.Zones;

internal struct SATimers
{
    public const uint BoatStart = 60 * Time.IN_MILLISECONDS;
    public const uint RoundLength = 600 * Time.IN_MILLISECONDS;
    public const uint WarmupLength = 120 * Time.IN_MILLISECONDS;
}