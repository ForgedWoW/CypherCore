// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Battlepay;

public enum BattlePayDistribution
{
    // character boost
    CharacterBoost = 2,

    CharacterBoostAllow = 1,
    CharacterBoostChoosed = 2,
    CharacterBoostItems = 3,
    CharacterBoostApplied = 4,
    CharacterBoostTextID = 88,
    CharacterBoostSpecMask = 0xFFF,
    CharacterBoostFactionAlliance = 0x1000000
}