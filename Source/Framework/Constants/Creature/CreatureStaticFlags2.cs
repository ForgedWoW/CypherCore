﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum CreatureStaticFlags2 : uint
{
	NO_PET_SCALING = 0x00000001,
	FORCE_PARTY_MEMBERS_INTO_COMBAT = 0x00000002, // Original description: Force Raid Combat
	LOCK_TAPPERS_TO_RAID_ON_DEATH = 0x00000004,   // "Lock Tappers To Raid On Death", toggleable by 'Set "RAID_LOCK_ON_DEATH" flag for unit(s)' action, CREATURE_FLAG_EXTRA_INSTANCE_BIND
	SPELL_ATTACKABLE = 0x00000008,                // CREATURE_TYPE_FLAG_SPELL_ATTACKABLE, original description(not valid anymore?): No Harmful Vertex Coloring
	NO_CRUSHING_BLOWS = 0x00000010,               // CREATURE_FLAG_EXTRA_NO_CRUSHING_BLOWS
	NO_OWNER_THREAT = 0x00000020,
	NO_WOUNDED_SLOWDOWN = 0x00000040,
	USE_CREATOR_BONUSES = 0x00000080,
	IGNORE_FEIGN_DEATH = 0x00000100, // CREATURE_FLAG_EXTRA_IGNORE_FEIGN_DEATH
	IGNORE_SANCTUARY = 0x00000200,
	ACTION_TRIGGERS_WHILE_CHARMED = 0x00000400,
	INTERACT_WHILE_DEAD = 0x00000800, // CREATURE_TYPE_FLAG_INTERACT_WHILE_DEAD
	NO_INTERRUPT_SCHOOL_COOLDOWN = 0x00001000,
	RETURN_SOUL_SHARD_TO_MASTER_OF_PET = 0x00002000,
	SKIN_WITH_HERBALISM = 0x00004000, // CREATURE_TYPE_FLAG_SKIN_WITH_HERBALISM
	SKIN_WITH_MINING = 0x00008000,    // CREATURE_TYPE_FLAG_SKIN_WITH_MINING
	ALERT_CONTENT_TEAM_ON_DEATH = 0x00010000,
	ALERT_CONTENT_TEAM_AT_90_PCT_HP = 0x00020000,
	ALLOW_MOUNTED_COMBAT = 0x00040000, // CREATURE_TYPE_FLAG_ALLOW_MOUNTED_COMBAT
	PVP_ENABLING_OOC = 0x00080000,
	NO_DEATH_MESSAGE = 0x00100000, // CREATURE_TYPE_FLAG_NO_DEATH_MESSAGE
	IGNORE_PATHING_FAILURE = 0x00200000,
	FULL_SPELL_LIST = 0x00400000,
	DOES_NOT_REDUCE_REPUTATION_FOR_RAIDS = 0x00800000,
	IGNORE_MISDIRECTION = 0x01000000,
	HIDE_BODY = 0x02000000, // UNIT_FLAGHIDE_BODY
	SPAWN_DEFENSIVE = 0x04000000,
	SERVER_ONLY = 0x08000000,
	CAN_SAFE_FALL = 0x10000000,  // Original description: No Collision
	CAN_ASSIST = 0x20000000,     // CREATURE_TYPE_FLAG_CAN_ASSIST, original description: Player Can Heal/Buff
	NO_SKILL_GAINS = 0x40000000, // CREATURE_FLAG_EXTRA_NO_SKILL_GAINS
	NO_PET_BAR = 0x80000000      // CREATURE_TYPE_FLAG_NO_PET_BAR
}