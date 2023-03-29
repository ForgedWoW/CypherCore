// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum ZLiquidStatus
{
    NoWater = 0x00,
    AboveWater = 0x01,
    WaterWalk = 0x02,
    InWater = 0x04,
    UnderWater = 0x08,

    Swimming = InWater | UnderWater,
    InContact = Swimming | WaterWalk
}