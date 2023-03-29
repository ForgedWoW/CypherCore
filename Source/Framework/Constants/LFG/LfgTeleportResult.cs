// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum LfgTeleportResult
{
    // 7 = "You Can'T Do That Right Now" | 5 = No Client Reaction
    None = 0, // Internal Use
    Dead = 1,
    Falling = 2,
    OnTransport = 3,
    Exhaustion = 4,
    NoReturnLocation = 6,
    ImmuneToSummons = 8 // Fixme - It Can Be 7 Or 8 (Need Proper Data)

    // unknown values
    //LFG_TELEPORT_RESULT_NOT_IN_DUNGEON,
    //LFG_TELEPORT_RESULT_NOT_ALLOWED,
    //LFG_TELEPORT_RESULT_ALREADY_IN_DUNGEON
}