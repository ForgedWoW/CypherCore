// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum CreatureStaticFlags6 : uint
{
    DO_NOT_AUTO_RESUMMON = 0x00000001,                // "Do not auto-resummon this companion creature"
    REPLACE_VISIBLE_UNIT_IF_AVAILABLE = 0x00000002,   // "Smooth Phasing: Replace visible unit if available"
    IGNORE_REALM_COALESCING_HIDING_CODE = 0x00000004, // "Ignore the realm coalescing hiding code (always show)"
    TAPS_TO_FACTION = 0x00000008,
    ONLY_QUESTGIVER_FOR_SUMMONER = 0x00000010,
    AI_COMBAT_RETURN_PRECISE = 0x00000020,
    HOME_REALM_ONLY_LOOT = 0x00000040,
    NO_INTERACT_RESPONSE = 0x00000080, // TFLAGUNK7
    NO_INITIAL_POWER = 0x00000100,
    DONT_CANCEL_CHANNEL_ON_MASTER_MOUNTING = 0x00000200,
    CAN_TOGGLE_BETWEEN_DEATH_AND_PERSONAL_LOOT = 0x00000400,
    ALWAYS_STAND_ON_TOP_OF_TARGET = 0x00000800, // "Always, ALWAYS tries to stand right on top of his move to target. ALWAYS!!", toggleable by 'Set "Always Stand on Target" flag for unit(s)' or not same?
    UNCONSCIOUS_ON_DEATH = 0x00001000,
    DONT_REPORT_TO_LOCAL_DEFENSE_CHANNEL_ON_DEATH = 0x00002000,
    PREFER_UNENGAGED_MONSTERS = 0x00004000,    // "Prefer unengaged monsters when picking a target"
    USE_PVP_POWER_AND_RESILIENCE = 0x00008000, // "Use PVP power and resilience when players attack this creature"
    DONT_CLEAR_DEBUFFS_ON_LEAVE_COMBAT = 0x00010000,
    PERSONAL_LOOT_HAS_FULL_SECURITY = 0x00020000, // "Personal loot has full security (guaranteed push/mail delivery)"
    TRIPLE_SPELL_VISUALS = 0x00040000,
    USE_GARRISON_OWNER_LEVEL = 0x00080000,
    IMMEDIATE_AOI_UPDATE_ON_SPAWN = 0x00100000,
    UI_CAN_GET_POSITION = 0x00200000,
    SEAMLESS_TRANSFER_PROHIBITED = 0x00400000,
    ALWAYS_USE_GROUP_LOOT_METHOD = 0x00800000,
    NO_BOSS_KILL_BANNER = 0x01000000,
    FORCE_TRIGGERING_PLAYER_LOOT_ONLY = 0x02000000,
    SHOW_BOSS_FRAME_WHILE_UNINTERACTABLE = 0x04000000,
    SCALES_TO_PLAYER_LEVEL = 0x08000000,
    AI_DONT_LEAVE_MELEE_FOR_RANGED_WHEN_TARGET_GETS_ROOTED = 0x10000000,
    DONT_USE_COMBAT_REACH_FOR_CHAINING = 0x20000000,
    DO_NOT_PLAY_PROCEDURAL_WOUND_ANIM = 0x40000000,
    APPLY_PROCEDURAL_WOUND_ANIM_TO_BASE = 0x80000000 // TFLAGUNK14
}