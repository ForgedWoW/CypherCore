// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellAttr5 : uint
{
    AllowActionsDuringChannel = 0x01,                           // Allow Actions During Channel
    NoReagentCostWithAura = 0x02,                               // No Reagent Cost With Aura
    RemoveEnteringArena = 0x04,                                 // Remove Entering Arena Description Force This Aura To Be Removed On Entering Arena, Regardless Of Other Properties
    AllowWhileStunned = 0x08,                                   // Allow While Stunned
    TriggersChanneling = 0x10,                                  // Triggers Channeling
    LimitN = 0x20,                                              // Limit N Description Remove Previous Application To Another Unit If Applied
    IgnoreAreaEffectPvpCheck = 0x40,                            // Ignore Area Effect Pvp Check
    NotOnPlayer = 0x80,                                         // Not On Player
    NotOnPlayerControlledNpc = 0x100,                           // Not On Player Controlled Npc
    ExtraInitialPeriod = 0x200,                                 // Extra Initial Period Description Immediately Do Periodic Tick On Apply
    DoNotDisplayDuration = 0x400,                               // Do Not Display Duration
    ImpliedTargeting = 0x800,                                   // Implied Targeting (Client Only)
    MeleeChainTargeting = 0x1000,                               // Melee Chain Targeting
    SpellHasteAffectsPeriodic = 0x2000,                         // Spell Haste Affects Periodic
    NotAvailableWhileCharmed = 0x4000,                          // Not Available While Charmed
    TreatAsAreaEffect = 0x8000,                                 // Treat As Area Effect
    AuraAffectsNotJustReqEquippedItem = 0x10000,                // Aura Affects Not Just Req. Equipped Item
    AllowWhileFleeing = 0x20000,                                // Allow While Fleeing
    AllowWhileConfused = 0x40000,                               // Allow While Confused
    AiDoesntFaceTarget = 0x80000,                               // Ai Doesn'T Face Target
    DoNotAttemptAPetResummonWhenDismounting = 0x100000, /*Nyi*/ // Do Not Attempt A Pet Resummon When Dismounting
    IgnoreTargetRequirements = 0x200000, /*Nyi*/                // Ignore Target Requirements
    NotOnTrivial = 0x400000, /*Nyi*/                            // Not On Trivial
    NoPartialResists = 0x800000, /*Nyi*/                        // No Partial Resists
    IgnoreCasterRequirements = 0x1000000, /*Nyi*/               // Ignore Caster Requirements
    AlwaysLineOfSight = 0x2000000,                              // Always Line Of Sight
    AlwaysAoeLineOfSight = 0x4000000,                           // Always Aoe Line Of Sight Description Requires Line Of Sight Between Caster And Target In Addition To Between Dest And Target
    NoCasterAuraIcon = 0x8000000,                               // No Caster Aura Icon (Client Only)
    NoTargetAuraIcon = 0x10000000,                              // No Target Aura Icon (Client Only)
    AuraUniquePerCaster = 0x20000000,                           // Aura Unique Per Caster
    AlwaysShowGroundTexture = 0x40000000,                       // Always Show Ground Texture
    AddMeleeHitRating = 0x80000000 /*Nyi*/                      // Add Melee Hit Rating
}