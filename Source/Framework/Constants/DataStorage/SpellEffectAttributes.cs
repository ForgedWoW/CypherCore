// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellEffectAttributes
{
    None = 0,
    UnaffectedByInvulnerability = 0x01, // not cancelled by immunities
    NoScaleWithStack = 0x40,
    ChainFromInitialTarget = 0x80,
    StackAuraAmountOnRecast = 0x8000, // refreshing periodic auras with this attribute will add remaining damage to new aura
    AllowAnyExplicitTarget = 0x100000,
    IgnoreDuringCooldownTimeRateCalculation = 0x800000
}