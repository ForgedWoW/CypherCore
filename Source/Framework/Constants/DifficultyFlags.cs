// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum DifficultyFlags : ushort
{
    Heroic = 0x01,
    Default = 0x02,
    CanSelect = 0x04, // Player can select this difficulty in dropdown menu
    ChallengeMode = 0x08,

    Legacy = 0x20,
    DisplayHeroic = 0x40,       // Controls icon displayed on minimap when inside the instance
    DisplayMythic = 0x80,       // Controls icon displayed on minimap when inside the instance
    DIFFICULTY_FLAG_UNK = 0x100 // Pvp and teeming island use
}