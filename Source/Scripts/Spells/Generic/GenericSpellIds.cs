// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Scripts.Spells.Generic;

internal struct GenericSpellIds
{
    // Adaptivewarding
    public const uint GEN_ADAPTIVE_WARDING_FIRE = 28765;
    public const uint GEN_ADAPTIVE_WARDING_NATURE = 28768;
    public const uint GEN_ADAPTIVE_WARDING_FROST = 28766;
    public const uint GEN_ADAPTIVE_WARDING_SHADOW = 28769;
    public const uint GEN_ADAPTIVE_WARDING_ARCANE = 28770;

    // Animalbloodpoolspell
    public const uint ANIMAL_BLOOD = 46221;
    public const uint SPAWN_BLOOD_POOL = 63471;

    // Serviceuniform
    public const uint SERVICE_UNIFORM = 71450;

    // Genericbandage
    public const uint RECENTLY_BANDAGED = 11196;

    // Bloodreserve
    public const uint BLOOD_RESERVE_AURA = 64568;
    public const uint BLOOD_RESERVE_HEAL = 64569;

    // Bonked
    public const uint BONKED = 62991;
    public const uint FORM_SWORD_DEFEAT = 62994;
    public const uint ONGUARD = 62972;

    // Breakshieldspells
    public const uint BREAK_SHIELD_DAMAGE2_K = 62626;
    public const uint BREAK_SHIELD_DAMAGE10_K = 64590;
    public const uint BREAK_SHIELD_TRIGGER_FACTION_MOUNTS = 62575; // Also On Toc5 Mounts
    public const uint BREAK_SHIELD_TRIGGER_CAMPAING_WARHORSE = 64595;
    public const uint BREAK_SHIELD_TRIGGER_UNK = 66480;

    // Cannibalizespells
    public const uint CANNIBALIZE_TRIGGERED = 20578;

    // Chaosblast
    public const uint CHAOS_BLAST = 37675;

    // Clone
    public const uint NIGHTMARE_FIGMENT_MIRROR_IMAGE = 57528;

    // Cloneweaponspells        
    public const uint WEAPON_AURA = 41054;
    public const uint WEAPON2_AURA = 63418;
    public const uint WEAPON3_AURA = 69893;

    public const uint OFFHAND_AURA = 45205;
    public const uint OFFHAND2_AURA = 69896;

    public const uint RANGED_AURA = 57594;

    // Createlancespells
    public const uint CREATE_LANCE_ALLIANCE = 63914;
    public const uint CREATE_LANCE_HORDE = 63919;

    // Dalarandisguisespells        
    public const uint SUNREAVER_TRIGGER = 69672;
    public const uint SUNREAVER_FEMALE = 70973;
    public const uint SUNREAVER_MALE = 70974;

    public const uint SILVER_COVENANT_TRIGGER = 69673;
    public const uint SILVER_COVENANT_FEMALE = 70971;
    public const uint SILVER_COVENANT_MALE = 70972;

    // Defendvisuals
    public const uint VISUAL_SHIELD1 = 63130;
    public const uint VISUAL_SHIELD2 = 63131;
    public const uint VISUAL_SHIELD3 = 63132;

    // Divinestormspell
    public const uint DIVINE_STORM = 53385;

    // Elunecandle
    public const uint OMEN_HEAD = 26622;
    public const uint OMEN_CHEST = 26624;
    public const uint OMEN_HAND_R = 26625;
    public const uint OMEN_HAND_L = 26649;
    public const uint NORMAL = 26636;

    // EtherealPet
    public const uint PROC_TRIGGER_ON_KILL_AURA = 50051;
    public const uint ETHEREAL_PET_AURA = 50055;
    public const uint CREATE_TOKEN = 50063;
    public const uint STEAL_ESSENCE_VISUAL = 50101;

    // Feast
    public const uint GREAT_FEAST = 57337;
    public const uint FISH_FEAST = 57397;
    public const uint GIGANTIC_FEAST = 58466;
    public const uint SMALL_FEAST = 58475;
    public const uint BOUNTIFUL_FEAST = 66477;

    public const uint FEAST_FOOD = 45548;
    public const uint FEAST_DRINK = 57073;
    public const uint BOUNTIFUL_FEAST_DRINK = 66041;
    public const uint BOUNTIFUL_FEAST_FOOD = 66478;

    public const uint GREAT_FEAST_REFRESHMENT = 57338;
    public const uint FISH_FEAST_REFRESHMENT = 57398;
    public const uint GIGANTIC_FEAST_REFRESHMENT = 58467;
    public const uint SMALL_FEAST_REFRESHMENT = 58477;
    public const uint BOUNTIFUL_FEAST_REFRESHMENT = 66622;

    //FuriousRage
    public const uint EXHAUSTION = 35492;

    // Fishingspells
    public const uint FISHING_NO_FISHING_POLE = 131476;
    public const uint FISHING_WITH_POLE = 131490;

    // Transporterbackfires
    public const uint TRANSPORTER_MALFUNCTION_POLYMORPH = 23444;
    public const uint TRANSPORTER_EVILTWIN = 23445;
    public const uint TRANSPORTER_MALFUNCTION_MISS = 36902;

    // Gnomishtransporter
    public const uint TRANSPORTER_SUCCESS = 23441;
    public const uint TRANSPORTER_FAILURE = 23446;

    // Interrupt
    public const uint GEN_THROW_INTERRUPT = 32747;

    // Genericlifebloomspells        
    public const uint HEXLORD_MALACRASS = 43422;
    public const uint TURRAGE_PAW = 52552;
    public const uint CENARION_SCOUT = 53692;
    public const uint TWISTED_VISAGE = 57763;
    public const uint FACTION_CHAMPIONS_DRU = 66094;

    // Chargespells        
    public const uint DAMAGE8_K5 = 62874;
    public const uint DAMAGE20_K = 68498;
    public const uint DAMAGE45_K = 64591;

    public const uint CHARGING_EFFECT8_K5 = 63661;
    public const uint CHARGING20_K1 = 68284;
    public const uint CHARGING20_K2 = 68501;
    public const uint CHARGING_EFFECT45_K1 = 62563;
    public const uint CHARGING_EFFECT45_K2 = 66481;

    public const uint TRIGGER_FACTION_MOUNTS = 62960;
    public const uint TRIGGER_TRIAL_CHAMPION = 68282;

    public const uint MISS_EFFECT = 62977;

    // MossCoveredFeet
    public const uint FALL_DOWN = 6869;

    // Netherbloom
    public const uint NETHER_BLOOM_POLLEN1 = 28703;

    // Nightmarevine
    public const uint NIGHTMARE_POLLEN = 28721;

    // Obsidianarmorspells        
    public const uint HOLY = 27536;
    public const uint FIRE = 27533;
    public const uint NATURE = 27538;
    public const uint FROST = 27534;
    public const uint SHADOW = 27535;
    public const uint ARCANE = 27540;

    // Tournamentpennantspells
    public const uint STORMWIND_ASPIRANT = 62595;
    public const uint STORMWIND_VALIANT = 62596;
    public const uint STORMWIND_CHAMPION = 62594;
    public const uint GNOMEREGAN_ASPIRANT = 63394;
    public const uint GNOMEREGAN_VALIANT = 63395;
    public const uint GNOMEREGAN_CHAMPION = 63396;
    public const uint SENJIN_ASPIRANT = 63397;
    public const uint SENJIN_VALIANT = 63398;
    public const uint SENJIN_CHAMPION = 63399;
    public const uint SILVERMOON_ASPIRANT = 63401;
    public const uint SILVERMOON_VALIANT = 63402;
    public const uint SILVERMOON_CHAMPION = 63403;
    public const uint DARNASSUS_ASPIRANT = 63404;
    public const uint DARNASSUS_VALIANT = 63405;
    public const uint DARNASSUS_CHAMPION = 63406;
    public const uint EXODAR_ASPIRANT = 63421;
    public const uint EXODAR_VALIANT = 63422;
    public const uint EXODAR_CHAMPION = 63423;
    public const uint IRONFORGE_ASPIRANT = 63425;
    public const uint IRONFORGE_VALIANT = 63426;
    public const uint IRONFORGE_CHAMPION = 63427;
    public const uint UNDERCITY_ASPIRANT = 63428;
    public const uint UNDERCITY_VALIANT = 63429;
    public const uint UNDERCITY_CHAMPION = 63430;
    public const uint ORGRIMMAR_ASPIRANT = 63431;
    public const uint ORGRIMMAR_VALIANT = 63432;
    public const uint ORGRIMMAR_CHAMPION = 63433;
    public const uint THUNDERBLUFF_ASPIRANT = 63434;
    public const uint THUNDERBLUFF_VALIANT = 63435;
    public const uint THUNDERBLUFF_CHAMPION = 63436;
    public const uint ARGENTCRUSADE_ASPIRANT = 63606;
    public const uint ARGENTCRUSADE_VALIANT = 63500;
    public const uint ARGENTCRUSADE_CHAMPION = 63501;
    public const uint EBONBLADE_ASPIRANT = 63607;
    public const uint EBONBLADE_VALIANT = 63608;
    public const uint EBONBLADE_CHAMPION = 63609;

    // Orcdisguisespells
    public const uint ORC_DISGUISE_TRIGGER = 45759;
    public const uint ORC_DISGUISE_MALE = 45760;
    public const uint ORC_DISGUISE_FEMALE = 45762;

    // Paralytic Poison
    public const uint PARALYSIS = 35202;

    // Parachutespells
    public const uint PARACHUTE = 45472;
    public const uint PARACHUTE_BUFF = 44795;

    // ProfessionResearch
    public const uint NORTHREND_INSCRIPTION_RESEARCH = 61177;

    // Trinketspells
    public const uint PVP_TRINKET_ALLIANCE = 97403;
    public const uint PVP_TRINKET_HORDE = 97404;

    // Replenishment
    public const uint REPLENISHMENT = 57669;
    public const uint INFINITE_REPLENISHMENT = 61782;

    // Runningwild
    public const uint ALTERED_FORM = 97709;

    // Seaforiumspells
    public const uint PLANT_CHARGES_CREDIT_ACHIEVEMENT = 60937;

    // Summonelemental
    public const uint SUMMON_FIRE_ELEMENTAL = 8985;
    public const uint SUMMON_EARTH_ELEMENTAL = 19704;

    // Tournamentmountsspells
    public const uint LANCE_EQUIPPED = 62853;

    // Mountedduelspells
    public const uint ON_TOURNAMENT_MOUNT = 63034;
    public const uint MOUNTED_DUEL = 62875;

    // Teleporting
    public const uint TELEPORT_SPIRE_DOWN = 59316;
    public const uint TELEPORT_SPIRE_UP = 59314;

    // Pvptrinkettriggeredspells
    public const uint WILL_OF_THE_FORSAKEN_COOLDOWN_TRIGGER = 72752;
    public const uint WILL_OF_THE_FORSAKEN_COOLDOWN_TRIGGER_WOTF = 72757;

    // Friendorfowl
    public const uint TURKEY_VENGEANCE = 25285;

    // Vampirictouch
    public const uint VAMPIRIC_TOUCH_HEAL = 52724;

    // Vehiclescaling
    public const uint GEAR_SCALING = 66668;

    // Whispergulchyoggsaronwhisper
    public const uint YOGG_SARON_WHISPER_DUMMY = 29072;

    // Gmfreeze
    public const uint GM_FREEZE = 9454;

    // Landmineknockbackachievement        
    public const uint LANDMINE_KNOCKBACK_ACHIEVEMENT = 57064;

    // Ponyspells
    public const uint ACHIEVEMENT_PONYUP = 3736;
    public const uint MOUNT_PONY = 29736;

    // CorruptinPlagueEntrys
    public const uint CORRUPTING_PLAGUE = 40350;

    // StasisFieldEntrys
    public const uint STASIS_FIELD = 40307;

    // SiegeTankControl
    public const uint SIEGE_TANK_CONTROL = 47963;

    // CannonBlast
    public const uint CANNON_BLAST = 42578;
    public const uint CANNON_BLAST_DAMAGE = 42576;

    // FreezingCircleMisc
    public const uint FREEZING_CIRCLE_PIT_OF_SARON_NORMAL = 69574;
    public const uint FREEZING_CIRCLE_PIT_OF_SARON_HEROIC = 70276;
    public const uint FREEZING_CIRCLE = 34787;
    public const uint FREEZING_CIRCLE_SCENARIO = 141383;

    // Kazrogalhellfiremark
    public const uint MARK_OF_KAZROGAL_HELLFIRE = 189512;
    public const uint MARK_OF_KAZROGAL_DAMAGE_HELLFIRE = 189515;

    // AuraprocRemovespells
    public const uint FACE_RAGE = 99947;
    public const uint IMPATIENT_MIND = 187213;

    // DefenderOfAzerothData
    public const uint DEATH_GATE_TELEPORT_STORMWIND = 316999;
    public const uint DEATH_GATE_TELEPORT_ORGRIMMAR = 317000;

    // AncestralCallSpells
    public const uint RICTUS_OF_THE_LAUGHING_SKULL = 274739;
    public const uint ZEAL_OF_THE_BURNING_BLADE = 274740;
    public const uint FEROCITY_OF_THE_FROSTWOLF = 274741;
    public const uint MIGHT_OF_THE_BLACKROCK = 274742;
}

// 430 Drink
// 431 Drink
// 432 Drink
// 1133 Drink
// 1135 Drink
// 1137 Drink
// 10250 Drink
// 22734 Drink
// 27089 Drink
// 34291 Drink
// 43182 Drink
// 43183 Drink
// 46755 Drink
// 49472 Drink Coffee
// 57073 Drink
// 61830 Drink

// 28865 - Consumption

// 50051 - Ethereal Pet Aura

// 50052 - Ethereal Pet onSummon

// 50055 - Ethereal Pet Aura Remove

// 50101 - Ethereal Pet OnKill Steal Essence

/* 57337 - Great Feast
57397 - Fish Feast
58466 - Gigantic Feast
58475 - Small Feast
66477 - Bountiful Feast */

/*
* There are only 3 possible flags Feign Death Auras can apply: UNIT_DYNFLAG_DEAD, UnitFlags2.FeignDeath
* and UNIT_FLAG_PREVENT_EMOTES_FROM_CHAT_TEXT. Some Auras can apply only 2 flags
* 
* spell_gen_feign_death_all_flags applies all 3 flags
* spell_gen_feign_death_no_dyn_flag applies no UNIT_DYNFLAG_DEAD (does not make the creature appear dead)
* spell_gen_feign_death_no_prevent_emotes applies no UNIT_FLAG_PREVENT_EMOTES_FROM_CHAT_TEXT
* 
* REACT_PASSIVE should be handled directly in scripts since not all creatures should be passive. Otherwise
* creature will be not able to aggro or execute MoveInLineOfSight events. Removing may cause more issues
* than already exists
*/

// 35357 - Spawn Feign Death

/* 9204 - Hate to Zero(Melee)
* 20538 - Hate to Zero(AoE)
* 26569 - Hate to Zero(AoE)
* 26637 - Hate to Zero(AoE, Unique)
* 37326 - Hate to Zero(AoE)
* 40410 - Hate to Zero(Should be added, AoE)
* 40467 - Hate to Zero(Should be added, AoE)
* 41582 - Hate to Zero(Should be added, Melee) */

// This spell is used by both player and creature, but currently works only if used by player

// 6870 Moss Covered Feet

// 23493 - Restoration

// 38772 Grievous Wound
// 43937 Grievous Wound
// 62331 Impale

// 31956 Grievous Wound
// 38801 Grievous Wound
// 43093 Grievous Throw
// 58517 Grievous Wound

// 70292 - Glacial Strike

// 52723 - Vampiric Touch

// BasePoints of spells is ID of npc_text used to group texts, it's not implemented so texts are grouped the old way
// 50037 - Mystery of the Infinite: Future You's Whisper to Controller - Random
// 50287 - Azure Dragon: On Death Force Cast Wyrmrest Defender to Whisper to Controller - Random
// 60709 - MOTI, Redux: Past You's Whisper to Controller - Random

// 40350 - Corrupting Plague

// 269083 - Enlisted

// Note: this spell unsummons any creature owned by the caster. Set appropriate Target conditions on the DB.
// 84065 - Despawn All Summons
// 83935 - Despawn All Summons

// 40307 - Stasis Field