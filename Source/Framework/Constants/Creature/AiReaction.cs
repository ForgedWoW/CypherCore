// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum AiReaction
{
    Alert = 0,    // pre-aggro (used in client packet handler)
    Friendly = 1, // (NOT used in client packet handler)
    Hostile = 2,  // sent on every attack, triggers aggro sound (used in client packet handler)
    Afraid = 3,   // seen for polymorph (when AI not in control of self?) (NOT used in client packet handler)
    Destory = 4   // used on object destroy (NOT used in client packet handler)
}