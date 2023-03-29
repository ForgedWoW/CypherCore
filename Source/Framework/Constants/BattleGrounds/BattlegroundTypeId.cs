// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum BattlegroundTypeId
{
    None = 0,          // None
    AV = 1,            // Alterac Valley
    WS = 2,            // Warsong Gulch
    AB = 3,            // Arathi Basin
    NA = 4,            // Nagrand Arena
    BE = 5,            // Blade'S Edge Arena
    AA = 6,            // All Arenas
    EY = 7,            // Eye Of The Storm
    RL = 8,            // Ruins Of Lordaernon
    SA = 9,            // Strand Of The Ancients
    DS = 10,           // Dalaran Sewers
    RV = 11,           // The Ring Of Valor
    IC = 30,           // Isle Of Conquest
    RB = 32,           // Random Battleground
    Rated10Vs10 = 100, // Rated Battleground 10 Vs 10
    Rated15Vs15 = 101, // Rated Battleground 15 Vs 15
    Rated25Vs25 = 102, // Rated Battleground 25 Vs 25
    TP = 108,          // Twin Peaks
    BFG = 120,         // Battle For Gilneas

    // 656 = "Rated Eye Of The Storm"
    Tk = 699, // Temple Of Kotmogu

    // 706 = "Ctf3"
    SM = 708,     // Silvershard Mines
    TVA = 719,    // Tol'Viron Arena
    DG = 754,     // Deepwind Gorge
    TTP = 757,    // The Tiger'S Peak
    SSvsTM = 789, // Southshore Vs. Tarren Mill
    SmallD = 803, // Small Battleground D
    BRH = 808,    // Black Rook Hold Arena

    // 809 = "New Nagrand Arena (Legion)"
    AF = 816, // Ashamane'S Fall

    // 844 = "New Blade'S Edge Arena (Legion)"
    BrawlTbg = 846, // Brawl - The Battle For Gilneas (Old City Map)
    BrawlAbw = 847, // Brawl - Arathi Basin Winter

    // 848 = "Ai Test - Arathi Basin"
    BrawlDd = 849,  // Brawl - Deepwind Dunk
    BrawlSps = 853, // Brawl - Shadow-Pan Showdown

    // 856 = "[Temp] Racetrackbg"
    Br = 857,       // Blackrock
    BrawlTh = 858,  // Brawl - Temple Of Hotmogu
    BrawlGl = 859,  // Brawl - Gravity Lapse
    BrawlDd2 = 860, // Brawl - Deepwind Dunk
    BrawlWs = 861,  // Brawl - Warsong Scramble
    BrawlEh = 862,  // Brawl - Eye Of The Horn
    BrawlAa = 866,  // Brawl - All Arenas
    Rl2 = 868,      // Ruins Of Lordaeron
    Ds2 = 869,      // Dalaran Sewers
    Tva2 = 870,     // Tol'Viron Arena
    Ttp2 = 871,     // The Tiger'S Peak
    Brha2 = 872,    // Black Rook Hold Arena
    Na2 = 873,      // Nagrand Arena
    Af2 = 874,      // Ashamane'S Fall
    Bea2 = 875,     // Blade'S Edge Arena

    // 878 = "Ai Test - Warsong Gulch"
    BrawlDs = 879,   // Brawl - Deep Six
    BrawlAb = 880,   // Brawl - Arathi Basin
    BrawlDg = 881,   // Brawl - Deepwind Gorge
    BrawlEs = 882,   // Brawl - Eye Of The Storm
    BrawlSm = 883,   // Brawl - Silvershard Mines
    BrawlTk = 884,   // Brawl - Temple Of Kotmogue
    BrawlTbg2 = 885, // Brawl - The Battle For Gilneas
    BrawlWg = 886,   // Brawl - Warsong Gulch
    Ci = 887,        // Cooking: Impossible
    DomSs = 890,     // Domination - Seething Strand

    // 893 = "8.0 Bg Temp"
    Ss = 894,         // Seething Shore
    Hp = 897,         // Hooking Point
    RandomEpic = 901, // Random Epic Battleground
    Ttp3 = 902,       // The Tiger'S Peak
    Mb = 903,         // Mugambala
    BrawlAa2 = 904,   // Brawl - All Arenas
    BrawlAash = 905,  // Brawl - All Arenas - Stocked House
    Af3 = 906,        // Ashamane'S Fall
    Bea3 = 907,       // Blade'S Edge Arena
    Be2 = 908,        // Blade'S Edge
    Ds3 = 909,        // Dalaran Sewers
    Na3 = 910,        // Nagrand Arena
    Rl3 = 911,        // Ruins Of Lordaeron
    Tva3 = 912,       // Tol'Viron Arena
    Brha3 = 913,      // Black Rook Hold Arena
    WgCtf = 1014,     // Warsong Gulch Capture The Flag
    EbBw = 1017,      // Epic Battleground - Battle For Wintergrasp
    DomAb = 1018,     // Domination - Arathi Basin
    AbCs = 1019,      // Arathi Basin Comp Stomp
    EbA = 1020,       // Epic Battleground - Ashran
    Ca = 1021,        // Classic Ashran (Endless)
    BrawlAb2 = 1022,  // Brawl - Arathi Basin
    Tr = 1025,        // The Robodrome (Arena)
    RandomBg = 1029,  // Random Battleground
    EbBw2 = 1030,     // Epic Battleground - Battle For Wintergrasp

    // 1031 = "Programmer Map - Battlefield"
    Kr = 1033,       // Korrak'S Revenge
    EpicBgWf = 1036, // Epic Battleground - Warfront Arathi (Pvp)
    DomDg = 1037,    // Domination - Deepwind Gorge
    DomDg2 = 1039,   // Domination - Deepwind Gorge
    Ed = 1041,       // Empyrean Domain
    Max = 902
}