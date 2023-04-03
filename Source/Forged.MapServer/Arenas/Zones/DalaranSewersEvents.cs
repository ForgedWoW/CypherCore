// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Arenas.Zones;

internal struct DalaranSewersEvents
{
    public const uint PIPE_KNOCKBACK = 5;
    public const uint WATERFALL_KNOCKBACK = 4;
    public const uint WATERFALL_OFF = 3;
    public const uint WATERFALL_ON = 2;

    public const int WATERFALL_WARNING = 1; // Water starting to fall, but no LoS Blocking nor movement blocking
    // LoS and Movement blocking active
}