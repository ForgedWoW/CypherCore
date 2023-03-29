// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SpellCooldownFlags
{
    None = 0x0,
    IncludeGCD = 0x1,            // Starts GCD in addition to normal cooldown specified in the packet
    IncludeEventCooldowns = 0x2, // Starts GCD for spells that should start their cooldown on events, requires SPELL_COOLDOWN_FLAG_INCLUDE_GCD set
    LossOfControlUi = 0x4,       // Shows interrupt cooldown in loss of control ui
    OnHold = 0x8                 // Forces cooldown to behave as if SpellInfo::IsCooldownStartedOnEvent was true
}