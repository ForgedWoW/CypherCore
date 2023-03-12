// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum CreatureStaticFlags4 : uint
{
	NO_BIRTH_ANIM = 0x00000001,                           // SMSG_UPDATE_OBJECT's "NoBirthAnim"
	TREAT_AS_PLAYER_FOR_DIMINISHING_RETURNS = 0x00000002, // Primarily used by ToC champions
	TREAT_AS_PLAYER_FOR_PVP_DEBUFF_DURATION = 0x00000004, // Primarily used by ToC champions
	INTERACT_ONLY_WITH_CREATOR = 0x00000008,              // CREATURE_TYPE_FLAG_INTERACT_ONLY_WITH_CREATOR, original description: Only Display Gossip for Summoner
	DO_NOT_PLAY_UNIT_EVENT_SOUNDS = 0x00000010,           // CREATURE_TYPE_FLAG_DO_NOT_PLAY_UNIT_EVENT_SOUNDS, original description: No Death Scream
	HAS_NO_SHADOW_BLOB = 0x00000020,                      // CREATURE_TYPE_FLAG_HAS_NO_SHADOW_BLOB, original description(wrongly linked type flag or behavior was changed?): Can be Healed by Enemies
	DEALS_TRIPLE_DAMAGE_TO_PC_CONTROLLED_PETS = 0x00000040,
	NO_NPC_DAMAGE_BELOW_85PTC = 0x00000080,
	OBEYS_TAUNT_DIMINISHING_RETURNS = 0x00000100, // CREATURE_FLAG_EXTRA_OBEYS_TAUNT_DIMINISHING_RETURNS
	NO_MELEE_APPROACH = 0x00000200,
	UPDATE_CREATURE_RECORD_WHEN_INSTANCE_CHANGES_DIFFICULTY = 0x00000400, // Used only by Snobold Vassal
	CANNOT_DAZE = 0x00000800,                                             // "Cannot Daze (Combat Stun)"
	FLAT_HONOR_AWARD = 0x00001000,
	IGNORE_LOS_WHEN_CASTING_ON_ME = 0x00002000, // "Other objects can ignore line of sight requirements when casting spells on me", used only by Ice Tomb in 3.3.5
	GIVE_QUEST_KILL_CREDIT_WHILE_OFFLINE = 0x00004000,
	TREAT_AS_RAID_UNIT_FOR_HELPFUL_SPELLS = 0x00008000,        // CREATURE_TYPE_FLAG_TREAT_AS_RAID_UNIT, "Treat as Raid Unit For Helpful Spells (Instances ONLY)", used by Valithria Dreamwalker
	DONT_REPOSITION_IF_MELEE_TARGET_IS_TOO_CLOSE = 0x00010000, // "Don't reposition because melee target is too close"
	PET_OR_GUARDIAN_AI_DONT_GO_BEHIND_TARGET = 0x00020000,
	MINUTE_LOOT_ROLL_TIMER = 0x00040000, // Used by Lich King
	FORCE_GOSSIP = 0x00080000,           // CREATURE_TYPE_FLAG_FORCE_GOSSIP
	DONT_REPOSITION_WITH_FRIENDS_IN_COMBAT = 0x00100000,
	DO_NOT_SHEATHE = 0x00200000,                      // CREATURE_TYPE_FLAG_DO_NOT_SHEATHE, original description: Manual Sheathing control
	IGNORE_SPELL_MIN_RANGE_RESTRICTIONS = 0x00400000, // UnitFlags2 0x8000000, original description: Attacker Ignores Minimum Ranges
	SUPPRESS_INSTANCE_WIDE_RELEASE_IN_COMBAT = 0x00800000,
	PREVENT_SWIM = 0x01000000,       // UnitFlags2 0x1000000, original description: AI will only swim if target swims
	HIDE_IN_COMBAT_LOG = 0x02000000, // UnitFlags2 0x2000000, original description: Don't generate combat log when engaged with NPC's
	ALLOW_NPC_COMBAT_WHILE_UNINTERACTIBLE = 0x04000000,
	PREFER_NPCS_WHEN_SEARCHING_FOR_ENEMIES = 0x08000000,
	ONLY_GENERATE_INITIAL_THREAT = 0x10000000,
	DO_NOT_TARGET_ON_INTERACTION = 0x20000000, // CREATURE_TYPE_FLAG_DO_NOT_TARGET_ON_INTERACTION, original description: Doesn't change target on right click
	DO_NOT_RENDER_OBJECT_NAME = 0x40000000,    // CREATURE_TYPE_FLAG_DO_NOT_RENDER_OBJECT_NAME, original description: Hide name in world frame
	QUEST_BOSS = 0x80000000                    // CREATURE_TYPE_FLAG_QUEST_BOSS
}