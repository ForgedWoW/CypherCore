// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SelectTargetMethod
{
    Random = 0,  // just pick a random target
    MaxThreat,   // prefer targets higher in the threat list
    MinThreat,   // prefer targets lower in the threat list
    MaxDistance, // prefer targets further from us
    MinDistance  // prefer targets closer to us
}