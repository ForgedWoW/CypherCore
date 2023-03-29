// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SummonPropertiesFlags : uint
{
    None = 0x00,
    AttackSummoner = 0x01,           // NYI
    HelpWhenSummonedInCombat = 0x02, // NYI
    UseLevelOffset = 0x04,           // NYI
    DespawnOnSummonerDeath = 0x08,   // NYI
    OnlyVisibleToSummoner = 0x10,
    CannotDismissPet = 0x20, // NYI
    UseDemonTimeout = 0x40,  // NYI
    UnlimitedSummons = 0x80, // NYI
    UseCreatureLevel = 0x100,
    JoinSummonerSpawnGroup = 0x200, // NYI
    DoNotToggle = 0x400,            // NYI
    DespawnWhenExpired = 0x800,     // NYI
    UseSummonerFaction = 0x1000,
    DoNotFollowMountedSummoner = 0x2000, // NYI
    SavePetAutocast = 0x4000,            // NYI
    IgnoreSummonerPhase = 0x8000,        // Wild Only
    OnlyVisibleToSummonerGroup = 0x10000,
    DespawnOnSummonerLogout = 0x20000,        // NYI
    CastRideVehicleSpellOnSummoner = 0x40000, // NYI
    GuardianActsLikePet = 0x80000,            // NYI
    DontSnapSessileToGround = 0x100000,       // NYI
    SummonFromBattlePetJournal = 0x200000,
    UnitClutter = 0x400000,                        // NYI
    DefaultNameColor = 0x800000,                   // NYI
    UseOwnInvisibilityDetection = 0x1000000,       // NYI. Ignore Owner's Invisibility Detection
    DespawnWhenReplaced = 0x2000000,               // NYI. Totem Slots Only
    DespawnWhenTeleportingOutOfRange = 0x4000000,  // NYI
    SummonedAtGroupFormationPosition = 0x8000000,  // NYI
    DontDespawnOnSummonerDeath = 0x10000000,       // NYI
    UseTitleAsCreatureName = 0x20000000,           // NYI
    AttackableBySummoner = 0x40000000,             // NYI
    DontDismissWhenEncounterIsAborted = 0x80000000 // NYI
}