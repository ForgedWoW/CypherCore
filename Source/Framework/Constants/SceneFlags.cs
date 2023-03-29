// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SceneFlags
{
    None = 0x00,
    PlayerNonInteractablePhased = 0x01, // Sets UNIT_FLAG_IMMUNE_TO_PC + UNIT_FLAG_IMMUNE_TO_NPC + UNIT_FLAG_PACIFIED
    FadeToBlackscreenOnComplete = 0x02,
    NotCancelable = 0x04,
    FadeToBlackscreenOnCancel = 0x08,

    IgnoreTransport = 0x20
}