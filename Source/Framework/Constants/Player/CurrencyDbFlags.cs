// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum CurrencyDbFlags
{
    None = 0x00,
    IgnoreMaxQtyOnload = 0x01,
    Reuse1 = 0x02,
    InBackpack = 0x04,
    UnusedInUI = 0x08,
    Reuse2 = 0x10,

    UnusedFlags = (IgnoreMaxQtyOnload | Reuse1 | Reuse2),
    ClientFlags = (0x1F & ~UnusedFlags)
}