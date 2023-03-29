// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellItemEnchantmentFlags : ushort
{
    Soulbound = 0x01,
    DoNotLog = 0x02,
    MainhandOnly = 0x04,
    AllowEnteringArena = 0x08,
    DoNotSaveToDB = 0x10,
    ScaleAsAGem = 0x20,
    DisableInChallengeModes = 0x40,
    DisableInProvingGrounds = 0x80,
    AllowTransmog = 0x100,
    HideUntilCollected = 0x200,
}