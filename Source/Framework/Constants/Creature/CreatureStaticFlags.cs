// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum CreatureStaticFlags : uint
{
    MOUNTABLE = 0x00000001,
    NO_XP = 0x00000002, // CREATURE_FLAG_EXTRA_NO_XP
    NO_LOOT = 0x00000004,
    UNKILLABLE = 0x00000008,
    TAMEABLE = 0x00000010,      // CREATURE_TYPE_FLAG_TAMEABLE
    IMMUNE_TO_PC = 0x00000020,  // UNIT_FLAG_IMMUNE_TO_PC
    IMMUNE_TO_NPC = 0x00000040, // UNIT_FLAG_IMMUNE_TO_NPC
    CAN_WIELD_LOOT = 0x00000080,
    SESSILE = 0x00000100,            // creature_template_movement.Rooted = 1
    UNINTERACTIBLE = 0x00000200,     // UNIT_FLAG_UNINTERACTIBLE
    NO_AUTOMATIC_REGEN = 0x00000400, // Creatures with that flag uses no UNIT_FLAGREGENERATE_POWER
    DESPAWN_INSTANTLY = 0x00000800,  // Creature instantly disappear when killed
    CORPSE_RAID = 0x00001000,
    CREATOR_LOOT = 0x00002000, // Lootable only by creator(engineering dummies)
    NO_DEFENSE = 0x00004000,
    NO_SPELL_DEFENSE = 0x00008000,
    BOSS_MOB = 0x00010000, // CREATURE_TYPE_FLAG_BOSS_MOB, original description: Raid Boss Mob
    COMBAT_PING = 0x00020000,
    AQUATIC = 0x00040000,                  // aka Water Only, creature_template_movement.Ground = 0
    AMPHIBIOUS = 0x00080000,               // creature_template_movement.Swim = 1
    NO_MELEE = 0x00100000,                 // Prevents melee(does not prevent chasing, does not make creature passive). Not sure what 'Flee' means but another flag is named NO_MELEE_APPROACH
    VISIBLE_TO_GHOSTS = 0x00200000,        // CREATURE_TYPE_FLAG_VISIBLE_TO_GHOSTS
    PVP_ENABLING = 0x00400000,             // Old UNIT_FLAG_PVP_ENABLING, now UNIT_BYTES_OFFSET_PVP_FLAG from UNIT_FIELD_BYTES_2
    DO_NOT_PLAY_WOUND_ANIM = 0x00800000,   // CREATURE_TYPE_FLAG_DO_NOT_PLAY_WOUND_ANIM
    NO_FACTION_TOOLTIP = 0x01000000,       // CREATURE_TYPE_FLAG_NO_FACTION_TOOLTIP
    IGNORE_COMBAT = 0x02000000,            // Actually only changes react state to passive
    ONLY_ATTACK_PVP_ENABLING = 0x04000000, // "Only attack targets that are PvP enabling"
    CALLS_GUARDS = 0x08000000,             // Creature will summon a guard if player is within its aggro range (even if creature doesn't attack per se)
    CAN_SWIM = 0x10000000,                 // UnitFlags 0x8000 UNIT_FLAG_CAN_SWIM
    FLOATING = 0x20000000,                 // creature_template_movement.Flight = 1
    MORE_AUDIBLE = 0x40000000,             // CREATURE_TYPE_FLAG_MORE_AUDIBLE
    LARGE_AOI = 0x80000000                 // UnitFlags2 0x200000
}