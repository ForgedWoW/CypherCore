// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum AnimTier
{
    Ground = 0, // plays ground tier animations
    Swim = 1,   // falls back to ground tier animations, not handled by the client, should never appear in sniffs, will prevent tier change animations from playing correctly if used
    Hover = 2,  // plays flying tier animations or falls back to ground tier animations, automatically enables hover clientside when entering visibility with this value
    Fly = 3,    // plays flying tier animations
    Submerged = 4,

    Max
}