// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum VictimState
{
    Intact = 0, // set when attacker misses
    Hit = 1,    // victim got clear/blocked hit
    Dodge = 2,
    Parry = 3,
    Imterrupt = 4,
    Blocks = 5, // unused? not set when blocked, even on full block
    Evades = 6,
    Immune = 7,
    Deflects = 8
}