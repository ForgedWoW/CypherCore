// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ChatFlags
{
    None = 0x00,
    AFK = 0x01,
    DND = 0x02,
    GM = 0x04,
    Com = 0x08, // Commentator
    Dev = 0x10,
    BossSound = 0x20, // Plays "RaidBossEmoteWarning" sound on raid boss emote/whisper
    Mobile = 0x40,
    Guide = 0x1000,
    Newcomer = 0x2000
}