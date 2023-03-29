// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellAuraInterruptFlags : uint
{
    None = 0,
    HostileActionReceived = 0x01,
    Damage = 0x02,
    Action = 0x04,
    Moving = 0x08,
    Turning = 0x10,
    Anim = 0x20,
    Dismount = 0x40,
    UnderWater = 0x80,  // TODO: disallow casting when swimming (SPELL_FAILED_ONLY_ABOVEWATER)
    AboveWater = 0x100, // TODO: disallow casting when not swimming (SPELL_FAILED_ONLY_UNDERWATER)
    Sheathing = 0x200,
    Interacting = 0x400, // TODO: more than gossip, replace all the feign death removals by aura type
    Looting = 0x800,
    Attacking = 0x1000,
    ItemUse = 0x2000,
    DamageChannelDuration = 0x4000,
    Shapeshifting = 0x8000,
    ActionDelayed = 0x10000,
    Mount = 0x20000,
    Standing = 0x40000,
    LeaveWorld = 0x80000,
    StealthOrInvis = 0x100000,
    InvulnerabilityBuff = 0x200000,
    EnterWorld = 0x400000,
    PvPActive = 0x800000,
    NonPeriodicDamage = 0x1000000,
    LandingOrFlight = 0x2000000,
    Release = 0x4000000,
    DamageCancelsScript = 0x8000000, // NYI dedicated aura script hook
    EnteringCombat = 0x10000000,
    Login = 0x20000000,
    Summon = 0x40000000,
    LeavingCombat = 0x80000000,

    NotVictim = (HostileActionReceived | Damage | NonPeriodicDamage)
}