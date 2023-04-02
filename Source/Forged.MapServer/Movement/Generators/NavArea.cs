// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Movement.Generators;

public enum NavArea
{
    Empty = 0,
    MagmaSlime = 8, // don't need to differentiate between them
    Water = 9,
    GroundSteep = 10,
    Ground = 11,
    MaxValue = Ground,
    MinValue = MagmaSlime,

    AllMask = 0x3F // max allowed value
    // areas 1-60 will be used for destructible areas (currently skipped in vmaps, WMO with flag 1)
    // ground is the highest value to make recast choose ground over water when merging surfaces very close to each other (shallow water would be walkable)
}