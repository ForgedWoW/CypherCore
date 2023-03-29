﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Database;

public enum HotfixStatements
{
    None = 0,

    SEL_ACHIEVEMENT,
    SEL_ACHIEVEMENT_LOCALE,

    SEL_ACHIEVEMENT_CATEGORY,
    SEL_ACHIEVEMENT_CATEGORY_LOCALE,

    SEL_ADVENTURE_JOURNAL,
    SEL_ADVENTURE_JOURNAL_LOCALE,

    SEL_ADVENTURE_MAP_POI,
    SEL_ADVENTURE_MAP_POI_LOCALE,

    SEL_ANIMATION_DATA,

    SEL_ANIM_KIT,

    SEL_AREA_GROUP_MEMBER,

    SEL_AREA_TABLE,
    SEL_AREA_TABLE_LOCALE,

    SEL_AREA_POI,
    SEL_AREA_POI_LOCALE,

    SEL_AREA_POI_STATE,
    SEL_AREA_POI_STATE_LOCALE,

    SEL_AREA_TRIGGER,

    SEL_ARMOR_LOCATION,

    SEL_ARTIFACT,
    SEL_ARTIFACT_LOCALE,

    SEL_ARTIFACT_APPEARANCE,
    SEL_ARTIFACT_APPEARANCE_LOCALE,

    SEL_ARTIFACT_APPEARANCE_SET,
    SEL_ARTIFACT_APPEARANCE_SET_LOCALE,

    SEL_ARTIFACT_CATEGORY,

    SEL_ARTIFACT_POWER,

    SEL_ARTIFACT_POWER_LINK,

    SEL_ARTIFACT_POWER_PICKER,

    SEL_ARTIFACT_POWER_RANK,

    SEL_ARTIFACT_QUEST_XP,

    SEL_ARTIFACT_TIER,

    SEL_ARTIFACT_UNLOCK,

    SEL_AUCTION_HOUSE,
    SEL_AUCTION_HOUSE_LOCALE,

    SEL_AZERITE_EMPOWERED_ITEM,

    SEL_AZERITE_ESSENCE,
    SEL_AZERITE_ESSENCE_LOCALE,

    SEL_AZERITE_ESSENCE_POWER,
    SEL_AZERITE_ESSENCE_POWER_LOCALE,

    SEL_AZERITE_ITEM,

    SEL_AZERITE_ITEM_MILESTONE_POWER,

    SEL_AZERITE_KNOWLEDGE_MULTIPLIER,

    SEL_AZERITE_LEVEL_INFO,

    SEL_AZERITE_POWER,

    SEL_AZERITE_POWER_SET_MEMBER,

    SEL_AZERITE_TIER_UNLOCK,

    SEL_AZERITE_TIER_UNLOCK_SET,

    SEL_AZERITE_UNLOCK_MAPPING,

    SEL_BANK_BAG_SLOT_PRICES,

    SEL_BANNED_ADDONS,

    SEL_BARBER_SHOP_STYLE,
    SEL_BARBER_SHOP_STYLE_LOCALE,

    SEL_BATTLE_PET_BREED_QUALITY,

    SEL_BATTLE_PET_BREED_STATE,

    SEL_BATTLE_PET_SPECIES,
    SEL_BATTLE_PET_SPECIES_LOCALE,

    SEL_BATTLE_PET_SPECIES_STATE,

    SEL_BATTLEMASTER_LIST,
    SEL_BATTLEMASTER_LIST_LOCALE,

    SEL_BROADCAST_TEXT,
    SEL_BROADCAST_TEXT_LOCALE,

    SEL_BROADCAST_TEXT_DURATION,

    SEL_CFG_REGIONS,

    SEL_CHAR_TITLES,
    SEL_CHAR_TITLES_LOCALE,

    SEL_CHARACTER_LOADOUT,

    SEL_CHARACTER_LOADOUT_ITEM,

    SEL_CHAT_CHANNELS,
    SEL_CHAT_CHANNELS_LOCALE,

    SEL_CHR_CLASS_UI_DISPLAY,

    SEL_CHR_CLASSES,
    SEL_CHR_CLASSES_LOCALE,

    SEL_CHR_CLASSES_X_POWER_TYPES,

    SEL_CHR_CUSTOMIZATION_CHOICE,
    SEL_CHR_CUSTOMIZATION_CHOICE_LOCALE,

    SEL_CHR_CUSTOMIZATION_DISPLAY_INFO,

    SEL_CHR_CUSTOMIZATION_ELEMENT,

    SEL_CHR_CUSTOMIZATION_OPTION,
    SEL_CHR_CUSTOMIZATION_OPTION_LOCALE,

    SEL_CHR_CUSTOMIZATION_REQ,
    SEL_CHR_CUSTOMIZATION_REQ_LOCALE,

    SEL_CHR_CUSTOMIZATION_REQ_CHOICE,

    SEL_CHR_MODEL,

    SEL_CHR_RACE_X_CHR_MODEL,

    SEL_CHR_RACES,
    SEL_CHR_RACES_LOCALE,

    SEL_CHR_SPECIALIZATION,
    SEL_CHR_SPECIALIZATION_LOCALE,

    SEL_CINEMATIC_CAMERA,

    SEL_CINEMATIC_SEQUENCES,

    SEL_CONTENT_TUNING,

    SEL_CONTENT_TUNING_X_EXPECTED,

    SEL_CONVERSATION_LINE,

    SEL_CORRUPTION_EFFECTS,

    SEL_CREATURE_DISPLAY_INFO,

    SEL_CREATURE_DISPLAY_INFO_EXTRA,

    SEL_CREATURE_FAMILY,
    SEL_CREATURE_FAMILY_LOCALE,

    SEL_CREATURE_MODEL_DATA,

    SEL_CREATURE_TYPE,
    SEL_CREATURE_TYPE_LOCALE,

    SEL_CRITERIA,

    SEL_CRITERIA_TREE,
    SEL_CRITERIA_TREE_LOCALE,

    SEL_CURRENCY_CONTAINER,
    SEL_CURRENCY_CONTAINER_LOCALE,

    SEL_CURRENCY_TYPES,
    SEL_CURRENCY_TYPES_LOCALE,

    SEL_CURVE,

    SEL_CURVE_POINT,

    SEL_DESTRUCTIBLE_MODEL_DATA,

    SEL_DIFFICULTY,
    SEL_DIFFICULTY_LOCALE,

    SEL_DUNGEON_ENCOUNTER,
    SEL_DUNGEON_ENCOUNTER_LOCALE,

    SEL_DURABILITY_COSTS,

    SEL_DURABILITY_QUALITY,

    SEL_EMOTES,

    SEL_EMOTES_TEXT,

    SEL_EMOTES_TEXT_SOUND,

    SEL_EXPECTED_STAT,

    SEL_EXPECTED_STAT_MOD,

    SEL_FACTION,
    SEL_FACTION_LOCALE,

    SEL_FACTION_TEMPLATE,

    SEL_FRIENDSHIP_REP_REACTION,
    SEL_FRIENDSHIP_REP_REACTION_LOCALE,

    SEL_FRIENDSHIP_REPUTATION,
    SEL_FRIENDSHIP_REPUTATION_LOCALE,

    SEL_GAMEOBJECT_ART_KIT,

    SEL_GAMEOBJECT_DISPLAY_INFO,

    SEL_GAMEOBJECTS,
    SEL_GAMEOBJECTS_LOCALE,

    SEL_GARR_ABILITY,
    SEL_GARR_ABILITY_LOCALE,

    SEL_GARR_BUILDING,
    SEL_GARR_BUILDING_LOCALE,

    SEL_GARR_BUILDING_PLOT_INST,

    SEL_GARR_CLASS_SPEC,
    SEL_GARR_CLASS_SPEC_LOCALE,

    SEL_GARR_FOLLOWER,
    SEL_GARR_FOLLOWER_LOCALE,

    SEL_GARR_FOLLOWER_X_ABILITY,

    SEL_GARR_MISSION,
    SEL_GARR_MISSION_LOCALE,

    SEL_GARR_PLOT,

    SEL_GARR_PLOT_BUILDING,

    SEL_GARR_PLOT_INSTANCE,

    SEL_GARR_SITE_LEVEL,

    SEL_GARR_SITE_LEVEL_PLOT_INST,

    SEL_GARR_TALENT_TREE,
    SEL_GARR_TALENT_TREE_LOCALE,

    SEL_GEM_PROPERTIES,

    SEL_GLOBAL_CURVE,

    SEL_GLYPH_BINDABLE_SPELL,

    SEL_GLYPH_PROPERTIES,

    SEL_GLYPH_REQUIRED_SPEC,

    SEL_GOSSIP_NPC_OPTION,

    SEL_GUILD_COLOR_BACKGROUND,

    SEL_GUILD_COLOR_BORDER,

    SEL_GUILD_COLOR_EMBLEM,

    SEL_GUILD_PERK_SPELLS,

    SEL_HEIRLOOM,
    SEL_HEIRLOOM_LOCALE,

    SEL_HOLIDAYS,

    SEL_IMPORT_PRICE_ARMOR,

    SEL_IMPORT_PRICE_QUALITY,

    SEL_IMPORT_PRICE_SHIELD,

    SEL_IMPORT_PRICE_WEAPON,

    SEL_ITEM,

    SEL_ITEM_APPEARANCE,

    SEL_ITEM_ARMOR_QUALITY,

    SEL_ITEM_ARMOR_SHIELD,

    SEL_ITEM_ARMOR_TOTAL,

    SEL_ITEM_BAG_FAMILY,
    SEL_ITEM_BAG_FAMILY_LOCALE,

    SEL_ITEM_BONUS,

    SEL_ITEM_BONUS_LIST_LEVEL_DELTA,

    SEL_ITEM_BONUS_TREE_NODE,

    SEL_ITEM_CHILD_EQUIPMENT,

    SEL_ITEM_CLASS,
    SEL_ITEM_CLASS_LOCALE,

    SEL_ITEM_CURRENCY_COST,

    SEL_ITEM_DAMAGE_AMMO,

    SEL_ITEM_DAMAGE_ONE_HAND,

    SEL_ITEM_DAMAGE_ONE_HAND_CASTER,

    SEL_ITEM_DAMAGE_TWO_HAND,

    SEL_ITEM_DAMAGE_TWO_HAND_CASTER,

    SEL_ITEM_DISENCHANT_LOOT,

    SEL_ITEM_EFFECT,

    SEL_ITEM_EXTENDED_COST,

    SEL_ITEM_LEVEL_SELECTOR,

    SEL_ITEM_LEVEL_SELECTOR_QUALITY,

    SEL_ITEM_LEVEL_SELECTOR_QUALITY_SET,

    SEL_ITEM_LIMIT_CATEGORY,
    SEL_ITEM_LIMIT_CATEGORY_LOCALE,

    SEL_ITEM_LIMIT_CATEGORY_CONDITION,

    SEL_ITEM_MODIFIED_APPEARANCE,

    SEL_ITEM_MODIFIED_APPEARANCE_EXTRA,

    SEL_ITEM_NAME_DESCRIPTION,
    SEL_ITEM_NAME_DESCRIPTION_LOCALE,

    SEL_ITEM_PRICE_BASE,

    SEL_ITEM_SEARCH_NAME,
    SEL_ITEM_SEARCH_NAME_LOCALE,

    SEL_ITEM_SET,
    SEL_ITEM_SET_LOCALE,

    SEL_ITEM_SET_SPELL,

    SEL_ITEM_SPARSE,
    SEL_ITEM_SPARSE_LOCALE,

    SEL_ITEM_SPEC,

    SEL_ITEM_SPEC_OVERRIDE,

    SEL_ITEM_X_BONUS_TREE,

    SEL_ITEM_X_ITEM_EFFECT,

    SEL_JOURNAL_ENCOUNTER,
    SEL_JOURNAL_ENCOUNTER_LOCALE,

    SEL_JOURNAL_ENCOUNTER_SECTION,
    SEL_JOURNAL_ENCOUNTER_SECTION_LOCALE,

    SEL_JOURNAL_INSTANCE,
    SEL_JOURNAL_INSTANCE_LOCALE,

    SEL_JOURNAL_TIER,
    SEL_JOURNAL_TIER_LOCALE,

    SEL_KEYCHAIN,

    SEL_KEYSTONE_AFFIX,
    SEL_KEYSTONE_AFFIX_LOCALE,

    SEL_LANGUAGE_WORDS,

    SEL_LANGUAGES,
    SEL_LANGUAGES_LOCALE,

    SEL_LFG_DUNGEONS,
    SEL_LFG_DUNGEONS_LOCALE,

    SEL_LIGHT,

    SEL_LIQUID_TYPE,

    SEL_LOCK,

    SEL_MAIL_TEMPLATE,
    SEL_MAIL_TEMPLATE_LOCALE,

    SEL_MAP,
    SEL_MAP_LOCALE,

    SEL_MAP_CHALLENGE_MODE,
    SEL_MAP_CHALLENGE_MODE_LOCALE,

    SEL_MAP_DIFFICULTY,
    SEL_MAP_DIFFICULTY_LOCALE,

    SEL_MAP_DIFFICULTY_X_CONDITION,
    SEL_MAP_DIFFICULTY_X_CONDITION_LOCALE,

    SEL_MAW_POWER,

    SEL_MODIFIER_TREE,

    SEL_MOUNT,
    SEL_MOUNT_LOCALE,

    SEL_MOUNT_CAPABILITY,

    SEL_MOUNT_TYPE_X_CAPABILITY,

    SEL_MOUNT_X_DISPLAY,

    SEL_MOVIE,

    SEL_NAME_GEN,

    SEL_NAMES_PROFANITY,

    SEL_NAMES_RESERVED,

    SEL_NAMES_RESERVED_LOCALE,

    SEL_NUM_TALENTS_AT_LEVEL,

    SEL_OVERRIDE_SPELL_DATA,

    SEL_PARAGON_REPUTATION,

    SEL_PHASE,

    SEL_PHASE_X_PHASE_GROUP,

    SEL_PLAYER_CONDITION,
    SEL_PLAYER_CONDITION_LOCALE,

    SEL_POWER_DISPLAY,

    SEL_POWER_TYPE,

    SEL_PRESTIGE_LEVEL_INFO,
    SEL_PRESTIGE_LEVEL_INFO_LOCALE,

    SEL_PVP_DIFFICULTY,

    SEL_PVP_ITEM,

    SEL_PVP_TALENT,
    SEL_PVP_TALENT_LOCALE,

    SEL_PVP_TALENT_CATEGORY,

    SEL_PVP_TALENT_SLOT_UNLOCK,

    SEL_PVP_TIER,
    SEL_PVP_TIER_LOCALE,

    SEL_QUEST_FACTION_REWARD,

    SEL_QUEST_INFO,
    SEL_QUEST_INFO_LOCALE,

    SEL_QUEST_P_O_I_BLOB,
    SEL_QUEST_P_O_I_POINT,

    SEL_QUEST_LINE_X_QUEST,

    SEL_QUEST_MONEY_REWARD,

    SEL_QUEST_PACKAGE_ITEM,

    SEL_QUEST_SORT,
    SEL_QUEST_SORT_LOCALE,

    SEL_QUEST_V2,

    SEL_QUEST_XP,

    SEL_RAND_PROP_POINTS,

    SEL_REWARD_PACK,

    SEL_REWARD_PACK_X_CURRENCY_TYPE,

    SEL_REWARD_PACK_X_ITEM,

    SEL_SCENARIO,
    SEL_SCENARIO_LOCALE,

    SEL_SCENARIO_STEP,
    SEL_SCENARIO_STEP_LOCALE,

    SEL_SCENE_SCRIPT,

    SEL_SCENE_SCRIPT_GLOBAL_TEXT,

    SEL_SCENE_SCRIPT_PACKAGE,

    SEL_SCENE_SCRIPT_TEXT,

    SEL_SKILL_LINE,
    SEL_SKILL_LINE_LOCALE,

    SEL_SKILL_LINE_ABILITY,
    SEL_SKILL_LINE_ABILITY_LOCALE,

    SEL_SKILL_LINE_X_TRAIT_TREE,

    SEL_SKILL_RACE_CLASS_INFO,

    SEL_SOULBIND_CONDUIT_RANK,

    SEL_SOUND_KIT,

    SEL_SPECIALIZATION_SPELLS,
    SEL_SPECIALIZATION_SPELLS_LOCALE,

    SEL_SPEC_SET_MEMBER,

    SEL_SPELL,

    SEL_SPELL_AURA_OPTIONS,

    SEL_SPELL_AURA_RESTRICTIONS,

    SEL_SPELL_CAST_TIMES,

    SEL_SPELL_CASTING_REQUIREMENTS,

    SEL_SPELL_CATEGORIES,

    SEL_SPELL_CATEGORY,
    SEL_SPELL_CATEGORY_LOCALE,

    SEL_SPELL_CLASS_OPTIONS,

    SEL_SPELL_COOLDOWNS,

    SEL_SPELL_DURATION,

    SEL_SPELL_EFFECT,

    SEL_SPELL_EMPOWER,

    SEL_SPELL_EMPOWER_STAGE,

    SEL_SPELL_EQUIPPED_ITEMS,

    SEL_SPELL_FOCUS_OBJECT,
    SEL_SPELL_FOCUS_OBJECT_LOCALE,

    SEL_SPELL_INTERRUPTS,

    SEL_SPELL_ITEM_ENCHANTMENT,
    SEL_SPELL_ITEM_ENCHANTMENT_LOCALE,

    SEL_SPELL_ITEM_ENCHANTMENT_CONDITION,

    SEL_SPELL_KEYBOUND_OVERRIDE,

    SEL_SPELL_LABEL,

    SEL_SPELL_LEARN_SPELL,

    SEL_SPELL_LEVELS,

    SEL_SPELL_MISC,

    SEL_SPELL_NAME,
    SEL_SPELL_NAME_LOCALE,

    SEL_SPELL_POWER,

    SEL_SPELL_POWER_DIFFICULTY,

    SEL_SPELL_PROCS_PER_MINUTE,

    SEL_SPELL_PROCS_PER_MINUTE_MOD,

    SEL_SPELL_RADIUS,

    SEL_SPELL_RANGE,
    SEL_SPELL_RANGE_LOCALE,

    SEL_SPELL_REAGENTS,

    SEL_SPELL_REAGENTS_CURRENCY,

    SEL_SPELL_REPLACEMENT,

    SEL_SPELL_SCALING,

    SEL_SPELL_SHAPESHIFT,

    SEL_SPELL_SHAPESHIFT_FORM,
    SEL_SPELL_SHAPESHIFT_FORM_LOCALE,

    SEL_SPELL_TARGET_RESTRICTIONS,

    SEL_SPELL_TOTEMS,

    SEL_SPELL_VISUAL,

    SEL_SPELL_VISUAL_EFFECT_NAME,

    SEL_SPELL_VISUAL_MISSILE,

    SEL_SPELL_VISUAL_KIT,

    SEL_SPELL_X_SPELL_VISUAL,

    SEL_SUMMON_PROPERTIES,

    SEL_TACT_KEY,

    SEL_TALENT,
    SEL_TALENT_LOCALE,

    SEL_TAXI_NODES,
    SEL_TAXI_NODES_LOCALE,

    SEL_TAXI_PATH,

    SEL_TAXI_PATH_NODE,

    SEL_TOTEM_CATEGORY,
    SEL_TOTEM_CATEGORY_LOCALE,

    SEL_TOY,
    SEL_TOY_LOCALE,

    SEL_TRANSMOG_HOLIDAY,

    SEL_TRAIT_COND,

    SEL_TRAIT_COST,

    SEL_TRAIT_CURRENCY,

    SEL_TRAIT_CURRENCY_SOURCE,
    SEL_TRAIT_CURRENCY_SOURCE_LOCALE,

    SEL_TRAIT_DEFINITION,
    SEL_TRAIT_DEFINITION_LOCALE,

    SEL_TRAIT_DEFINITION_EFFECT_POINTS,

    SEL_TRAIT_EDGE,

    SEL_TRAIT_NODE,

    SEL_TRAIT_NODE_ENTRY,

    SEL_TRAIT_NODE_ENTRY_X_TRAIT_COND,

    SEL_TRAIT_NODE_ENTRY_X_TRAIT_COST,

    SEL_TRAIT_NODE_GROUP,

    SEL_TRAIT_NODE_GROUP_X_TRAIT_COND,

    SEL_TRAIT_NODE_GROUP_X_TRAIT_COST,

    SEL_TRAIT_NODE_GROUP_X_TRAIT_NODE,

    SEL_TRAIT_NODE_X_TRAIT_COND,

    SEL_TRAIT_NODE_X_TRAIT_COST,

    SEL_TRAIT_NODE_X_TRAIT_NODE_ENTRY,

    SEL_TRAIT_SYSTEM,

    SEL_TRAIT_TREE,

    SEL_TRAIT_TREE_LOADOUT,

    SEL_TRAIT_TREE_LOADOUT_ENTRY,

    SEL_TRAIT_TREE_X_TRAIT_COST,

    SEL_TRAIT_TREE_X_TRAIT_CURRENCY,

    SEL_TRANSMOG_ILLUSION,

    SEL_TRANSMOG_SET,
    SEL_TRANSMOG_SET_LOCALE,

    SEL_TRANSMOG_SET_GROUP,
    SEL_TRANSMOG_SET_GROUP_LOCALE,

    SEL_TRANSMOG_SET_ITEM,

    SEL_TRANSPORT_ANIMATION,

    SEL_TRANSPORT_ROTATION,

    SEL_UI_MAP,
    SEL_UI_MAP_LOCALE,

    SEL_UI_MAP_ASSIGNMENT,

    SEL_UI_MAP_LINK,

    SEL_UI_MAP_X_MAP_ART,

    SEL_UI_SPLASH_SCREEN,
    SEL_UI_SPLASH_SCREEN_LOCALE,

    SEL_UNIT_CONDITION,

    SEL_UNIT_POWER_BAR,
    SEL_UNIT_POWER_BAR_LOCALE,

    SEL_VEHICLE,

    SEL_VEHICLE_SEAT,

    SEL_WMO_AREA_TABLE,
    SEL_WMO_AREA_TABLE_LOCALE,

    SEL_WORLD_EFFECT,

    SEL_WORLD_MAP_OVERLAY,

    SEL_WORLD_STATE_EXPRESSION,

    SEL_CHAR_BASE_INFO,
    MAX_HOTFIXDATABASE_STATEMENTS
}