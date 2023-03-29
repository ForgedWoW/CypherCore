// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PlayerFlags : uint
{
    GroupLeader = 0x01,
    AFK = 0x02,
    DND = 0x04,
    GM = 0x08,
    Ghost = 0x10,
    Resting = 0x20,
    VoiceChat = 0x40,
    Unk7 = 0x80,
    ContestedPVP = 0x100,
    InPVP = 0x200,
    WarModeActive = 0x400,
    WarModeDesired = 0x800,
    PlayedLongTime = 0x1000,
    PlayedTooLong = 0x2000,
    IsOutOfBounds = 0x4000,
    Developer = 0x8000,
    LowLevelRaidEnabled = 0x10000,
    TaxiBenchmark = 0x20000,
    PVPTimer = 0x40000,
    Uber = 0x80000,
    Unk20 = 0x100000,
    Unk21 = 0x200000,
    Commentator2 = 0x400000,
    HidAccountAchievements = 0x800000,
    PetBattlesUnlocked = 0x1000000,
    NoXPGain = 0x2000000,
    Unk26 = 0x4000000,
    AutoDeclineGuild = 0x8000000,
    GuildLevelEnabled = 0x10000000,
    VoidUnlocked = 0x20000000,
    Timewalking = 0x40000000,
    CommentatorCamera = 0x80000000
}