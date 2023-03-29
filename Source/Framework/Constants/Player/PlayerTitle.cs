// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PlayerTitle : ulong
{
    Disabled = 0x0000000000000000,
    None = 0x0000000000000001,
    Private = 0x0000000000000002,             // 1
    Corporal = 0x0000000000000004,            // 2
    SergeantA = 0x0000000000000008,           // 3
    MasterSergeant = 0x0000000000000010,      // 4
    SergeantMajor = 0x0000000000000020,       // 5
    Knight = 0x0000000000000040,              // 6
    KnightLieutenant = 0x0000000000000080,    // 7
    KnightCaptain = 0x0000000000000100,       // 8
    KnightChampion = 0x0000000000000200,      // 9
    LieutenantCommander = 0x0000000000000400, // 10
    Commander = 0x0000000000000800,           // 11
    Marshal = 0x0000000000001000,             // 12
    FieldMarshal = 0x0000000000002000,        // 13
    GrandMarshal = 0x0000000000004000,        // 14
    Scout = 0x0000000000008000,               // 15
    Grunt = 0x0000000000010000,               // 16
    SergeantH = 0x0000000000020000,           // 17
    SeniorSergeant = 0x0000000000040000,      // 18
    FirstSergeant = 0x0000000000080000,       // 19
    StoneGuard = 0x0000000000100000,          // 20
    BloodGuard = 0x0000000000200000,          // 21
    Legionnaire = 0x0000000000400000,         // 22
    Centurion = 0x0000000000800000,           // 23
    Champion = 0x0000000001000000,            // 24
    LieutenantGeneral = 0x0000000002000000,   // 25
    General = 0x0000000004000000,             // 26
    Warlord = 0x0000000008000000,             // 27
    HighWarlord = 0x0000000010000000,         // 28
    Gladiator = 0x0000000020000000,           // 29
    Duelist = 0x0000000040000000,             // 30
    Rival = 0x0000000080000000,               // 31
    Challenger = 0x0000000100000000,          // 32
    ScarabLord = 0x0000000200000000,          // 33
    Conqueror = 0x0000000400000000,           // 34
    Justicar = 0x0000000800000000,            // 35
    ChampionOfTheNaaru = 0x0000001000000000,  // 36
    MercilessGladiator = 0x0000002000000000,  // 37
    OfTheShatteredSun = 0x0000004000000000,   // 38
    HandOfAdal = 0x0000008000000000,          // 39
    VengefulGladiator = 0x0000010000000000,   // 40
}