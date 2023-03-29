// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ResetFailedReason
{
    Failed = 0,  // "Cannot reset %s.  There are players still inside the instance."
    Offline = 1, // "Cannot reset %s.  There are players offline in your party."
    Zoning = 2   // "Cannot reset %s.  There are players in your party attempting to zone into an instance."
}