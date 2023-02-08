﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Dynamic;
using Game.Scripting.BaseScripts;

namespace Scripts.Spells.DeathKnight
{
    internal struct DeathKnightSpells
    {
        public const uint ArmyFleshBeastTransform = 127533;
        public const uint ArmyGeistTransform = 127534;
        public const uint ArmyNorthrendSkeletonTransform = 127528;
        public const uint ArmySkeletonTransform = 127527;
        public const uint ArmySpikedGhoulTransform = 127525;
        public const uint ArmySuperZombieTransform = 127526;
        public const uint Blood = 137008;
        public const uint BloodPlague = 55078;
        public const uint BloodShieldAbsorb = 77535;
        public const uint BloodShieldMastery = 77513;
        public const uint CorpseExplosionTriggered = 43999;
        public const uint DeathAndDecayDamage = 52212;
        public const uint DeathCoilDamage = 47632;
        public const uint DeathGripDummy = 243912;
        public const uint DeathGripJump = 49575;
        public const uint DeathGripTaunt = 51399;
        public const uint DeathStrikeEnabler = 89832; //Server Side
        public const uint DeathStrikeHeal = 45470;
        public const uint DeathStrikeOffhand = 66188;
        public const uint FesteringWound = 194310;
        public const uint Frost = 137006;
        public const uint FrostFever = 55095;
        public const uint FrostScythe = 207230;
        public const uint GlyphOfFoulMenagerie = 58642;
        public const uint GlyphOfTheGeist = 58640;
        public const uint GlyphOfTheSkeleton = 146652;
        public const uint MarkOfBloodHeal = 206945;
        public const uint NecrosisEffect = 216974;
        public const uint RaiseDeadSummon = 52150;
        public const uint RecentlyUsedDeathStrike = 180612;
        public const uint RunicPowerEnergize = 49088;
        public const uint RunicReturn = 61258;
        public const uint SludgeBelcher = 207313;
        public const uint SludgeBelcherSummon = 212027;
        public const uint TighteningGrasp = 206970;
        public const uint TighteningGraspSlow = 143375;
        public const uint Unholy = 137007;
        public const uint UnholyVigor = 196263;
        public const uint VolatileShielding = 207188;
        public const uint VolatileShieldingDamage = 207194;
        public const uint SPELL_DK_ARMY_FLESH_BEAST_TRANSFORM = 127533;
        public const uint SPELL_DK_ARMY_GEIST_TRANSFORM = 127534;
        public const uint SPELL_DK_ARMY_NORTHREND_SKELETON_TRANSFORM = 127528;
        public const uint SPELL_DK_ARMY_SKELETON_TRANSFORM = 127527;
        public const uint SPELL_DK_ARMY_SPIKED_GHOUL_TRANSFORM = 127525;
        public const uint SPELL_DK_ARMY_SUPER_ZOMBIE_TRANSFORM = 127526;
        public const uint SPELL_DK_BLOOD_PLAGUE = 55078;
        public const uint SPELL_DK_BLOOD_PRESENCE = 48263;
        public const uint SPELL_DK_BLOOD_SHIELD_MASTERY = 77513;
        public const uint SPELL_DK_BLOOD_SHIELD_ABSORB = 77535;
        public const uint SPELL_DK_CHAINS_OF_ICE = 45524;
        public const uint SPELL_DK_CORPSE_EXPLOSION_TRIGGERED = 43999;
        public const uint SPELL_DK_DEATH_AND_DECAY_DAMAGE = 52212;
        public const uint SPELL_DK_DEATH_AND_DECAY_SLOW = 143375;
        public const uint SPELL_DK_DEATH_COIL_BARRIER = 115635;
        public const uint SPELL_DK_DEATH_COIL_DAMAGE = 47632;
        public const uint SPELL_DK_DEATH_COIL_HEAL = 47633;
        public const uint SPELL_DK_DEATH_GRIP = 49576;
        public const uint SPELL_DK_DEATH_GRIP_PULL = 49575;
        public const uint SPELL_DK_DEATH_GRIP_VISUAL = 55719;
        public const uint SPELL_DK_DEATH_GRIP_TAUNT = 57603;
        public const uint SPELL_DK_DEATH_STRIKE = 49998;
        public const uint SPELL_DK_DEATH_STRIKE_HEAL = 45470;
        public const uint SPELL_DK_DECOMPOSING_AURA = 199720;
        public const uint SPELL_DK_DECOMPOSING_AURA_DAMAGE = 199721;
        public const uint SPELL_DK_ENHANCED_DEATH_COIL = 157343;
        public const uint SPELL_DK_FROST_FEVER = 55095;
        public const uint SPELL_DK_GHOUL_EXPLODE = 47496;
        public const uint SPELL_DK_GLYPH_OF_ABSORB_MAGIC = 159415;
        public const uint SPELL_DK_GLYPH_OF_ANTI_MAGIC_SHELL = 58623;
        public const uint SPELL_DK_GLYPH_OF_ARMY_OF_THE_DEAD = 58669;
        public const uint SPELL_DK_GLYPH_OF_DEATH_COIL = 63333;
        public const uint SPELL_DK_GLYPH_OF_DEATH_AND_DECAY = 58629;
        public const uint SPELL_DK_GLYPH_OF_FOUL_MENAGERIE = 58642;
        public const uint SPELL_DK_GLYPH_OF_REGENERATIVE_MAGIC = 146648;
        public const uint SPELL_DK_GLYPH_OF_RUNIC_POWER_TRIGGERED = 159430;
        public const uint SPELL_DK_GLYPH_OF_SWIFT_DEATH = 146645;
        public const uint SPELL_DK_GLYPH_OF_THE_GEIST = 58640;
        public const uint SPELL_DK_GLYPH_OF_THE_SKELETON = 146652;
        public const uint SPELL_DK_IMPROVED_BLOOD_PRESENCE = 50371;
        public const uint SPELL_DK_IMPROVED_SOUL_REAPER = 157342;
        public const uint SPELL_DK_RUNIC_POWER_ENERGIZE = 49088;
        public const uint SPELL_DK_SCENT_OF_BLOOD = 49509;
        public const uint SPELL_DK_SCENT_OF_BLOOD_TRIGGERED = 50421;
        public const uint SPELL_DK_SCOURGE_STRIKE_TRIGGERED = 70890;
        public const uint SPELL_DK_SHADOW_OF_DEATH = 164047;
        public const uint SPELL_DK_SOUL_REAPER_DAMAGE = 114867;
        public const uint SPELL_DK_SOUL_REAPER_HASTE = 114868;
        public const uint spell_dk_soul_reaper = 343294;
        public const uint SPELL_DK_T15_DPS_4P_BONUS = 138347;
        public const uint SPELL_DK_UNHOLY_PRESENCE = 48265;
        public const uint SPELL_DK_WILL_OF_THE_NECROPOLIS = 206967;
        public const uint SPELL_DK_BLOOD_BOIL_TRIGGERED = 65658;
        public const uint SPELL_DK_BLOOD_GORGED_HEAL = 50454;
        public const uint SPELL_DK_DEATH_STRIKE_ENABLER = 89832;
        public const uint SPELL_DK_FROST_PRESENCE = 48266;
        public const uint SPELL_DK_IMPROVED_FROST_PRESENCE = 50385;
        public const uint SPELL_DK_IMPROVED_FROST_PRESENCE_TRIGGERED = 50385;
        public const uint SPELL_DK_IMPROVED_UNHOLY_PRESENCE = 50392;
        public const uint SPELL_DK_IMPROVED_UNHOLY_PRESENCE_TRIGGERED = 55222;
        public const uint SPELL_DK_RUNE_TAP = 48982;
        public const uint SPELL_DK_CORPSE_EXPLOSION_VISUAL = 51270;
        public const uint SPELL_DK_TIGHTENING_GRASP = 206970;
        public const uint SPELL_DK_TIGHTENING_GRASP_SLOW = 143375;
        public const uint SPELL_DK_MASTER_OF_GHOULS = 52143;
        public const uint SPELL_DK_GHOUL_AS_GUARDIAN = 46585;
        public const uint SPELL_DK_GHOUL_AS_PET = 52150;
        public const uint SPELL_DK_ROILING_BLOOD = 108170;
        public const uint SPELL_DK_CHILBLAINS = 50041;
        public const uint SPELL_DK_CHAINS_OF_ICE_ROOT = 53534;
        public const uint SPELL_DK_PLAGUE_LEECH = 123693;
        public const uint SPELL_DK_PERDITION = 123981;
        public const uint SPELL_DK_SHROUD_OF_PURGATORY = 116888;
        public const uint SPELL_DK_PURGATORY_INSTAKILL = 123982;
        public const uint SPELL_DK_BLOOD_RITES = 50034;
        public const uint SPELL_DK_DEATH_SIPHON_HEAL = 116783;
        public const uint SPELL_DK_BLOOD_CHARGE = 114851;
        public const uint SPELL_DK_BOOD_TAP = 45529;
        public const uint SPELL_DK_PILLAR_OF_FROST = 51271;
        public const uint SPELL_DK_CONVERSION = 119975;
        public const uint SPELL_DK_WEAKENED_BLOWS = 115798;
        public const uint SPELL_DK_SCARLET_FEVER = 81132;
        public const uint SPELL_DK_SCENT_OF_BLOOD_AURA = 50421;
        public const uint SPELL_DK_DESECRATED_GROUND = 118009;
        public const uint SPELL_DK_DESECRATED_GROUND_IMMUNE = 115018;
        public const uint SPELL_DK_ASPHYXIATE = 108194;
        public const uint SPELL_DK_DARK_INFUSION_STACKS = 91342;
        public const uint SPELL_DK_DARK_INFUSION_AURA = 93426;
        public const uint SPELL_DK_RUNIC_CORRUPTION_REGEN = 51460;
        public const uint SPELL_DK_RUNIC_EMPOWERMENT = 81229;
        public const uint SPELL_DK_GOREFIENDS_GRASP_GRIP_VISUAL = 114869;
        public const uint SPELL_DK_SLUDGE_BELCHER_AURA = 207313;
        public const uint SPELL_DK_SLUDGE_BELCHER_ABOMINATION = 212027;
        public const uint spell_dk_raise_dead = 46584;
        public const uint spell_dk_raise_dead_GHOUL = 52150;
        public const uint SPELL_DK_GEIST_TRANSFORM = 121916;
        public const uint SPELL_DK_ANTI_MAGIC_BARRIER = 205725;
        public const uint SPELL_DK_RUNIC_CORRUPTION = 51462;
        public const uint SPELL_DK_NECROSIS = 207346;
        public const uint SPELL_DK_NECROSIS_EFFECT = 216974;
        public const uint SPELL_DK_ALL_WILL_SERVE = 194916;
        public const uint SPELL_DK_ALL_WILL_SERVE_SUMMON = 196910;
        public const uint SPELL_DK_BREATH_OF_SINDRAGOSA = 152279;
        public const uint SPELL_DK_DEATH_GRIP_ONLY_JUMP = 146599;
        public const uint SPELL_DK_HEART_STRIKE = 206930;
        public const uint SPELL_DK_FESTERING_WOUND = 194310;
        public const uint SPELL_DK_FESTERING_WOUND_DAMAGE = 194311;
        public const uint SPELL_DK_BONE_SHIELD = 195181;
        public const uint SPELL_DK_BLOOD_MIRROR_DAMAGE = 221847;
        public const uint SPELL_DK_BLOOD_MIRROR = 206977;
        public const uint SPELL_DK_BONESTORM_HEAL = 196545;
        public const uint SPELL_DK_GLACIAL_ADVANCE = 194913;
        public const uint SPELL_DK_GLACIAL_ADVANCE_DAMAGE = 195975;
        public const uint SPELL_DK_HOWLING_BLAST = 49184;
        public const uint SPELL_DK_HOWLING_BLAST_AREA_DAMAGE = 237680;
        public const uint SPELL_DK_RIME_BUFF = 59052;
        public const uint SPELL_DK_NORTHREND_WINDS = 204088;
        public const uint SPELL_DK_KILLING_MACHINE = 51124;
        public const uint SPELL_DK_REMORSELESS_WINTER_SLOW_DOWN = 211793;
        public const uint SPELL_DK_EPIDEMIC = 207317;
        public const uint SPELL_DK_EPIDEMIC_DAMAGE_SINGLE = 212739;
        public const uint SPELL_DK_EPIDEMIC_DAMAGE_AOE = 215969;
        public const uint SPELL_DK_VIRULENT_PLAGUE = 191587;
        public const uint SPELL_DK_VIRULENT_ERUPTION = 191685;
        public const uint SPELL_DK_OUTBREAK_PERIODIC = 196782;
        public const uint SPELL_DK_DEFILE = 152280;
        public const uint SPELL_DK_DEFILE_DAMAGE = 156000;
        public const uint SPELL_DK_DEFILE_DUMMY = 156004;
        public const uint SPELL_DK_DEFILE_MASTERY = 218100;
        public const uint SPELL_DK_UNHOLY_FRENZY = 207289;
        public const uint SPELL_DK_UNHOLY_FRENZY_BUFF = 207290;
        public const uint SPELL_DK_PESTILENT_PUSTULES = 194917;
        public const uint SPELL_DK_CASTIGATOR = 207305;
        public const uint SPELL_DK_UNHOLY_VIGOR = 196263;
        public const uint SPELL_DK_RECENTLY_USED_DEATH_STRIKE = 180612;
        public const uint SPELL_DK_FROST = 137006;
        public const uint SPELL_DK_DEATH_STRIKE_OFFHAND = 66188;
        public const uint SPELL_DK_VAMPIRIC_BLOOD = 55233;
        public const uint SPELL_DK_CRIMSOM_SCOURGE = 81136;
        public const uint SPELL_DK_CLAWING_SHADOWS = 207311;
        public const uint SPELL_DK_INFECTED_CLAWS = 207272;
        public const uint SPELL_DK_SUMMON_DEFILE = 169018;
        public const uint SPELL_DK_HARBINGER_OF_THE_DOOM = 276023;
        public const uint SPELL_DK_PESTILENCE = 277234;
        public const uint SPELL_DK_ANTIMAGIC_ZONE_DAMAGE_TAKEN = 145629;
        public const uint SPELL_DK_DARK_SIMULACRUM = 77606;
        public const uint SPELL_DK_DARK_SIMULACRUM_PROC = 77616;
        public const uint SPELL_DK_ICECAP = 207126;
        public const uint SPELL_DK_COLD_HEART_CHARGE = 281209;
        public const uint SPELL_DK_COLD_HEART_DAMAGE = 281210;
        public const uint SPELL_DK_OSSUARY_MOD_MAX_POWER = 219786;
        public const uint SPELL_DK_OSSUARY_MOD_POWER_COST = 219788;
        public const uint SPELL_DK_GRIP_OF_THE_DEAD = 273952;
        public const uint SPELL_DK_GRIP_OF_THE_DEAD_PERIODIC = 273980;
        public const uint SPELL_DK_GRIP_OF_THE_DEAD_SLOW = 273977;
        public const uint SPELL_DK_VORACIOUS = 273953;
        public const uint SPELL_DK_VORACIOUS_MOD_LEECH = 274009;
        public const uint SPELL_DK_RED_THIRST = 205723;
        public const uint SPELL_DK_PURGATORY = 114556;
        public const uint SPELL_DK_BONESTORM = 194844;
        public const uint SPELL_DK_INEXORABLE_ASSAULT_DUMMY = 253593;
        public const uint SPELL_DK_INEXORABLE_ASSAULT_STACK = 253595;
        public const uint SPELL_DK_INEXORABLE_ASSAULT_DAMAGE = 253597;
        public const uint SPELL_DK_FROSTSCYTHE = 207230;
        public const uint SPELL_DK_AVALANCHE = 207142;
        public const uint SPELL_DK_AVALANCHE_DAMAGE = 207150;
        public const uint SPELL_DK_RIME = 59057;
        public const uint SPELL_DK_RAZORICE_MOD_DAMAGE_TAKEN = 51714;
        public const uint SPELL_DK_OBLITERATION = 281238;
        public const uint SPELL_DK_ARMY_OF_THE_DAMNED = 276837;
        public const uint SPELL_DK_RUNIC_CORRUPTION_MOD_RUNES = 51460;
        public const uint SPELL_DK_ARMY_OF_THE_DEAD = 42650;
        public const uint SPELL_DK_APOCALYPSE = 275699;
        public const uint SPELL_DK_DARK_TRANSFORMATION = 63560;
        public const uint SPELL_DK_SOUL_REAPER_MOD_HASTE = 69410;

        public static uint[] ArmyTransforms =
        {
            ArmyFleshBeastTransform, ArmyGeistTransform, ArmyNorthrendSkeletonTransform, ArmySkeletonTransform, ArmySpikedGhoulTransform, ArmySuperZombieTransform
        };
    }
}