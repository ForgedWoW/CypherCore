// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum CreatureFlagsExtra : uint
{
    InstanceBind = 0x01,                    // Creature Kill Bind Instance With Killer And Killer'S Group
    Civilian = 0x02,                        // Not Aggro (Ignore Faction/Reputation Hostility)
    NoParry = 0x04,                         // Creature Can'T Parry
    NoParryHasten = 0x08,                   // Creature Can'T Counter-Attack At Parry
    NoBlock = 0x10,                         // Creature Can'T Block
    NoCrushingBlows = 0x20,                 // Creature Can'T Do Crush Attacks
    NoXP = 0x40,                            // creature kill does not provide XP
    Trigger = 0x80,                         // Trigger Creature
    NoTaunt = 0x100,                        // Creature Is Immune To Taunt Auras And 'attack me' effects
    NoMoveFlagsUpdate = 0x200,              // Creature won't update movement flags
    GhostVisibility = 0x400,                // creature will only be visible to dead players
    UseOffhandAttack = 0x800,               // creature will use offhand attacks
    NoSellVendor = 0x1000,                  // players can't sell items to this vendor
    CannotEnterCombat = 0x2000,             // creature is not allowed to enter combat
    Worldevent = 0x4000,                    // Custom Flag For World Event Creatures (Left Room For Merging)
    Guard = 0x8000,                         // Creature Is Guard
    IgnoreFeighDeath = 0x10000,             // creature ignores feign death
    NoCrit = 0x20000,                       // Creature Can'T Do Critical Strikes
    NoSkillGains = 0x40000,                 // creature won't increase weapon skills
    ObeysTauntDiminishingReturns = 0x80000, // Taunt is subject to diminishing returns on this creature
    AllDiminish = 0x100000,                 // creature is subject to all diminishing returns as players are
    NoPlayerDamageReq = 0x200000,           // creature does not need to take player damage for kill credit
    Unused22 = 0x400000,
    Unused23 = 0x800000,
    Unused24 = 0x1000000,
    Unused25 = 0x2000000,
    Unused26 = 0x4000000,
    Unused27 = 0x8000000,
    DungeonBoss = 0x10000000,       // Creature Is A Dungeon Boss (Set Dynamically, Do Not Add In Db)
    IgnorePathfinding = 0x20000000, // creature ignore pathfinding
    ImmunityKnockback = 0x40000000, // creature is immune to knockback effects
    Unused31 = 0x80000000,

    // Masks
    AllUnused = (Unused22 | Unused23 | Unused24 | Unused25 | Unused26 | Unused27 | Unused31),

    DBAllowed = (0xFFFFFFFF & ~(AllUnused | DungeonBoss))
}