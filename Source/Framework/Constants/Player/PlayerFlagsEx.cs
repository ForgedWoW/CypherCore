// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PlayerFlagsEx
{
    ReagentBankUnlocked = 0x01,
    MercenaryMode = 0x02,
    ArtifactForgeCheat = 0x04,
    InPvpCombat = 0x0040, // Forbids /Follow
    Mentor = 0x0080,
    Newcomer = 0x0100,
    UnlockedAoeLoot = 0x0200
}