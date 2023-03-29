// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum LiquidHeaderTypeFlags : byte
{
    NoWater = 0x00,
    Water = 0x01,
    Ocean = 0x02,
    Magma = 0x04,
    Slime = 0x08,

    DarkWater = 0x10,

    AllLiquids = Water | Ocean | Magma | Slime
}