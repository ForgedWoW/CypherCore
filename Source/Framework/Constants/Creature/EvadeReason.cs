// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum EvadeReason
{
    NoHostiles,    // the creature's threat list is empty
    Boundary,      // the creature has moved outside its evade boundary
    NoPath,        // the creature was unable to reach its target for over 5 seconds
    SequenceBreak, // this is a boss and the pre-requisite encounters for engaging it are not defeated yet
    Other
}