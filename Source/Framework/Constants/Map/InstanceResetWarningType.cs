// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum InstanceResetWarningType
{
    WarningHours = 1,   // WARNING! %s is scheduled to reset in %d hour(s).
    WarningMin = 2,     // WARNING! %s is scheduled to reset in %d minute(s)!
    WarningMinSoon = 3, // WARNING! %s is scheduled to reset in %d minute(s). Please exit the zone or you will be returned to your bind location!
    Welcome = 4,        // Welcome to %s. This raid instance is scheduled to reset in %s.
    Expired = 5
}