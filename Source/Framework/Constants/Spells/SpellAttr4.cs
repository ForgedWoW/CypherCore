// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellAttr4 : uint
{
    NoCastLog = 0x01,                         // No Cast Log
    ClassTriggerOnlyOnTarget = 0x02,          // Class Trigger Only On Target
    AuraExpiresOffline = 0x04,                // Aura Expires Offline Description Debuffs (Except Resurrection Sickness) Will Automatically Do This
    NoHelpfulThreat = 0x08,                   // No Helpful Threat
    NoHarmfulThreat = 0x10,                   // No Harmful Threat
    AllowClientTargeting = 0x20,              // Allow Client Targeting Description Allows Client To Send Spell Targets For This Spell. Applies Only To Pet Spells, Without This Attribute CmsgPetAction Is Sent Instead Of CmsgPetCastSpell
    CannotBeStolen = 0x40,                    // Cannot Be Stolen
    AllowCastWhileCasting = 0x80,             // Allow Cast While Casting Description Ignores Already In-Progress Cast And Still Casts
    IgnoreDamageTakenModifiers = 0x100,       // Ignore Damage Taken Modifiers
    CombatFeedbackWhenUsable = 0x200,         // Combat Feedback When Usable (Client Only)
    WeaponSpeedCostScaling = 0x400,           // Weapon Speed Cost Scaling Description Adds 10 To Power Cost For Each 1s Of Weapon Speed
    NoPartialImmunity = 0x800,                // No Partial Immunity
    AuraIsBuff = 0x1000,                      // Aura Is Buff
    DoNotLogCaster = 0x2000,                  // Do Not Log Caster
    ReactiveDamageProc = 0x4000,              // Reactive Damage Proc Description Damage From Spells With This Attribute Doesn'T Break Auras That Normally Break On Damage Taken
    NotInSpellbook = 0x8000,                  // Not In Spellbook
    NotInArenaOrRatedBattleground = 0x10000,  // Not In Arena Or Rated Battleground Description Makes Spell Unusable Despite Cd <= 10min
    IgnoreDefaultArenaRestrictions = 0x20000, // Ignore Default Arena Restrictions Description Makes Spell Usable Despite Cd > 10min
    BouncyChainMissiles = 0x40000,            // Bouncy Chain Missiles Description Hits Area Targets Over Time Instead Of All At Once
    AllowProcWhileSitting = 0x80000,          // Allow Proc While Sitting
    AuraNeverBounces = 0x100000,              // Aura Never Bounces
    AllowEnteringArena = 0x200000,            // Allow Entering Arena
    ProcSuppressSwingAnim = 0x400000,         // Proc Suppress Swing Anim
    SuppressWeaponProcs = 0x800000,           // Suppress Weapon Procs
    AutoRangedCombat = 0x1000000,             // Auto Ranged Combat
    OwnerPowerScaling = 0x2000000,            // Owner Power Scaling
    OnlyFlyingAreas = 0x4000000,              // Only Flying Areas
    ForceDisplayCastbar = 0x8000000,          // Force Display Castbar
    IgnoreCombatTimer = 0x10000000,           // Ignore Combat Timer
    AuraBounceFailsSpell = 0x20000000,        // Aura Bounce Fails Spell
    Obsolete = 0x40000000,                    // Obsolete
    UseFacingFromSpell = 0x80000000           // Use Facing From Spell
}