// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Arenas.Zones;

internal struct DalaranSewersData
{
    public const uint NpcWaterSpout = 28567;

    public const uint PipeKnockbackTotalCount = 2;

    public static TimeSpan PipeKnockbackDelay = TimeSpan.FromSeconds(3);

    public static TimeSpan PipeKnockbackFirstDelay = TimeSpan.FromSeconds(5);

    public static TimeSpan WaterfallDuration = TimeSpan.FromSeconds(30);

    public static TimeSpan WaterfallKnockbackTimer = TimeSpan.FromSeconds(1.5);

    public static TimeSpan WaterfallTimerMax = TimeSpan.FromSeconds(60);

    // These values are NOT blizzlike... need the correct data!
    public static TimeSpan WaterfallTimerMin = TimeSpan.FromSeconds(30);

    public static TimeSpan WaterWarningDuration = TimeSpan.FromSeconds(5);
}