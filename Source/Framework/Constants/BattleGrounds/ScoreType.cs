// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ScoreType
{
    KillingBlows = 1,
    Deaths = 2,
    HonorableKills = 3,
    BonusHonor = 4,
    DamageDone = 5,
    HealingDone = 6,

    // Ws And Ey
    FlagCaptures = 7,
    FlagReturns = 8,

    // Ab And Ic
    BasesAssaulted = 9,
    BasesDefended = 10,

    // Av
    GraveyardsAssaulted = 11,
    GraveyardsDefended = 12,
    TowersAssaulted = 13,
    TowersDefended = 14,
    MinesCaptured = 15,

    // Sota
    DestroyedDemolisher = 16,
    DestroyedWall = 17
}