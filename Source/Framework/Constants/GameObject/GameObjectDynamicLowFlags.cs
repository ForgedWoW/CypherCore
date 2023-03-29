// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum GameObjectDynamicLowFlags : ushort
{
    HideModel = 0x02,
    Activate = 0x04,
    Animate = 0x08,
    Depleted = 0x10,
    Sparkle = 0x20,
    Stopped = 0x40,
    NoInterract = 0x80,
    InvertedMovement = 0x100,
    Highlight = 0x200
}