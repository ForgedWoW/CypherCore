// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum RaidGroupReason
{
    None = 0,
    Lowlevel = 1,           // "You are too low level to enter this instance."
    Only = 2,               // "You must be in a raid group to enter this instance."
    Full = 3,               // "The instance is full."
    RequirementsUnmatch = 4 // "You do not meet the requirements to enter this instance."
}