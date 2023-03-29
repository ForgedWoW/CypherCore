// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum UnitConditionVariable
{
    None = 0,                              // - NONE -
    Race = 1,                              // Race {$Is/Is Not} "{ChrRaces}"
    Class = 2,                             // Class {$Is/Is Not} "{ChrClasses}"
    Level = 3,                             // Level {$Relative Op} "{#Level}"
    IsSelf = 4,                            // Is self? {$Yes/No}{=1}
    IsMyPet = 5,                           // Is my pet? {$Yes/No}{=1}
    IsMaster = 6,                          // Is master? {$Yes/No}{=1}
    IsTarget = 7,                          // Is target? {$Yes/No}{=1}
    CanAssist = 8,                         // Can assist? {$Yes/No}{=1}
    CanAttack = 9,                         // Can attack? {$Yes/No}{=1}
    HasPet = 10,                           // Has pet? {$Yes/No}{=1}
    HasWeapon = 11,                        // Has weapon? {$Yes/No}{=1}
    HealthPct = 12,                        // Health {$Relative Op} {#Health %}%
    ManaPct = 13,                          // Mana {$Relative Op} {#Mana %}%
    RagePct = 14,                          // Rage {$Relative Op} {#Rage %}%
    EnergyPct = 15,                        // Energy {$Relative Op} {#Energy %}%
    ComboPoints = 16,                      // Combo Points {$Relative Op} {#Points}
    HasHelpfulAuraSpell = 17,              // Has helpful aura spell? {$Yes/No} "{Spell}"
    HasHelpfulAuraDispelType = 18,         // Has helpful aura dispel type? {$Yes/No} "{SpellDispelType}"
    HasHelpfulAuraMechanic = 19,           // Has helpful aura mechanic? {$Yes/No} "{SpellMechanic}"
    HasHarmfulAuraSpell = 20,              // Has harmful aura spell? {$Yes/No} "{Spell}"
    HasHarmfulAuraDispelType = 21,         // Has harmful aura dispel type? {$Yes/No} "{SpellDispelType}"
    HasHarmfulAuraMechanic = 22,           // Has harmful aura mechanic? {$Yes/No} "{SpellMechanic}"
    HasHarmfulAuraSchool = 23,             // Has harmful aura school? {$Yes/No} "{Resistances}"
    DamagePhysicalPct = 24,                // NYI Damage (Physical) {$Relative Op} {#Physical Damage %}%
    DamageHolyPct = 25,                    // NYI Damage (Holy) {$Relative Op} {#Holy Damage %}%
    DamageFirePct = 26,                    // NYI Damage (Fire) {$Relative Op} {#Fire Damage %}%
    DamageNaturePct = 27,                  // NYI Damage (Nature) {$Relative Op} {#Nature Damage %}%
    DamageFrostPct = 28,                   // NYI Damage (Frost) {$Relative Op} {#Frost Damage %}%
    DamageShadowPct = 29,                  // NYI Damage (Shadow) {$Relative Op} {#Shadow Damage %}%
    DamageArcanePct = 30,                  // NYI Damage (Arcane) {$Relative Op} {#Arcane Damage %}%
    InCombat = 31,                         // In combat? {$Yes/No}{=1}
    IsMoving = 32,                         // Is moving? {$Yes/No}{=1}
    IsCasting = 33,                        // Is casting? {$Yes/No}{=1}
    IsCastingSpell = 34,                   // Is casting spell? {$Yes/No}{=1}
    IsChanneling = 35,                     // Is channeling? {$Yes/No}{=1}
    IsChannelingSpell = 36,                // Is channeling spell? {$Yes/No}{=1}
    NumberOfMeleeAttackers = 37,           // Number of melee attackers {$Relative Op} {#Attackers}
    IsAttackingMe = 38,                    // Is attacking me? {$Yes/No}{=1}
    Range = 39,                            // Range {$Relative Op} {#Yards}
    InMeleeRange = 40,                     // In melee range? {$Yes/No}{=1}
    PursuitTime = 41,                      // NYI Pursuit time {$Relative Op} {#Seconds}
    HasHarmfulAuraCanceledByDamage = 42,   // Has harmful aura canceled by damage? {$Yes/No}{=1}
    HasHarmfulAuraWithPeriodicDamage = 43, // Has harmful aura with periodic damage? {$Yes/No}{=1}
    NumberOfEnemies = 44,                  // Number of enemies {$Relative Op} {#Enemies}
    NumberOfFriends = 45,                  // NYI Number of friends {$Relative Op} {#Friends}
    ThreatPhysicalPct = 46,                // NYI Threat (Physical) {$Relative Op} {#Physical Threat %}%
    ThreatHolyPct = 47,                    // NYI Threat (Holy) {$Relative Op} {#Holy Threat %}%
    ThreatFirePct = 48,                    // NYI Threat (Fire) {$Relative Op} {#Fire Threat %}%
    ThreatNaturePct = 49,                  // NYI Threat (Nature) {$Relative Op} {#Nature Threat %}%
    ThreatFrostPct = 50,                   // NYI Threat (Frost) {$Relative Op} {#Frost Threat %}%
    ThreatShadowPct = 51,                  // NYI Threat (Shadow) {$Relative Op} {#Shadow Threat %}%
    ThreatArcanePct = 52,                  // NYI Threat (Arcane) {$Relative Op} {#Arcane Threat %}%
    IsInterruptible = 53,                  // NYI Is interruptible? {$Yes/No}{=1}
    NumberOfAttackers = 54,                // Number of attackers {$Relative Op} {#Attackers}
    NumberOfRangedAttackers = 55,          // Number of ranged attackers {$Relative Op} {#Ranged Attackers}
    CreatureType = 56,                     // Creature type {$Is/Is Not} "{CreatureType}"
    IsMeleeAttacking = 57,                 // Is melee-attacking? {$Yes/No}{=1}
    IsRangedAttacking = 58,                // Is ranged-attacking? {$Yes/No}{=1}
    Health = 59,                           // Health {$Relative Op} {#HP} HP
    SpellKnown = 60,                       // Spell known? {$Yes/No} "{Spell}"
    HasHarmfulAuraEffect = 61,             // Has harmful aura effect? {$Yes/No} "{#Spell Aura}"
    IsImmuneToAreaOfEffect = 62,           // NYI Is immune to area-of-effect? {$Yes/No}{=1}
    IsPlayer = 63,                         // Is player? {$Yes/No}{=1}
    DamageMagicPct = 64,                   // NYI Damage (Magic) {$Relative Op} {#Magic Damage %}%
    DamageTotalPct = 65,                   // NYI Damage (Total) {$Relative Op} {#Damage %}%
    ThreatMagicPct = 66,                   // NYI Threat (Magic) {$Relative Op} {#Magic Threat %}%
    ThreatTotalPct = 67,                   // NYI Threat (Total) {$Relative Op} {#Threat %}%
    HasCritter = 68,                       // Has critter? {$Yes/No}{=1}
    HasTotemInSlot1 = 69,                  // Has totem in slot 1? {$Yes/No}{=1}
    HasTotemInSlot2 = 70,                  // Has totem in slot 2? {$Yes/No}{=1}
    HasTotemInSlot3 = 71,                  // Has totem in slot 3? {$Yes/No}{=1}
    HasTotemInSlot4 = 72,                  // Has totem in slot 4? {$Yes/No}{=1}
    HasTotemInSlot5 = 73,                  // NYI Has totem in slot 5? {$Yes/No}{=1}
    Creature = 74,                         // Creature {$Is/Is Not} "{Creature}"
    StringID = 75,                         // NYI String ID {$Is/Is Not} "{StringID}"
    HasAura = 76,                          // Has aura? {$Yes/No} {Spell}
    IsEnemy = 77,                          // Is enemy? {$Yes/No}{=1}
    IsSpecMelee = 78,                      // Is spec - melee? {$Yes/No}{=1}
    IsSpecTank = 79,                       // Is spec - tank? {$Yes/No}{=1}
    IsSpecRanged = 80,                     // Is spec - ranged? {$Yes/No}{=1}
    IsSpecHealer = 81,                     // Is spec - healer? {$Yes/No}{=1}
    IsPlayerControlledNPC = 82,            // Is player controlled NPC? {$Yes/No}{=1}
    IsDying = 83,                          // Is dying? {$Yes/No}{=1}
    PathFailCount = 84,                    // NYI Path fail count {$Relative Op} {#Path Fail Count}
    IsMounted = 85,                        // Is mounted? {$Yes/No}{=1}
    Label = 86,                            // NYI Label {$Is/Is Not} "{Label}"
    IsMySummon = 87,                       //
    IsSummoner = 88,                       //
    IsMyTarget = 89,                       //
    Sex = 90,                              // Sex {$Is/Is Not} "{UnitSex}"
    LevelWithinContentTuning = 91,         // Level is within {$Is/Is Not} {ContentTuning}

    IsFlying = 93,             // Is flying? {$Yes/No}{=1}
    IsHovering = 94,           // Is hovering? {$Yes/No}{=1}
    HasHelpfulAuraEffect = 95, // Has helpful aura effect? {$Yes/No} "{#Spell Aura}"
    HasHelpfulAuraSchool = 96, // Has helpful aura school? {$Yes/No} "{Resistances}"
}