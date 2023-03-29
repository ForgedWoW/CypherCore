// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum PlayerUnderwaterState
{
    None = 0x00,
    InWater = 0x01,     // terrain type is water and player is afflicted by it
    InLava = 0x02,      // terrain type is lava and player is afflicted by it
    InSlime = 0x04,     // terrain type is lava and player is afflicted by it
    InDarkWater = 0x08, // terrain type is dark water and player is afflicted by it

    ExistTimers = 0x10
}