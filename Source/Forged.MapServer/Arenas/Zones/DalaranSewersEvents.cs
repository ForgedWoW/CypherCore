// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Arenas.Zones;

internal struct DalaranSewersEvents
{
    public const uint PipeKnockback = 5;
    public const uint WaterfallKnockback = 4;
    public const uint WaterfallOff = 3;
    public const uint WaterfallOn = 2;

    public const int WaterfallWarning = 1; // Water starting to fall, but no LoS Blocking nor movement blocking
    // LoS and Movement blocking active
}