// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum CurrencyGainFlags
{
    None = 0x00,
    BonusAward = 0x01,
    DroppedFromDeath = 0x02,
    FromAccountServer = 0x04
}