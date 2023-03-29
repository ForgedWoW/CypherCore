﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum CreatureStaticFlags7 : uint
{
    IMPORTANT_NPC = 0x00000001,
    IMPORTANT_QUEST_NPC = 0x00000002,
    LARGE_NAMEPLATE = 0x00000004,
    TRIVIAL_PET = 0x00000008,
    AI_ENEMIES_DONT_BACKUP_WHEN_I_GET_ROOTED = 0x00000010,
    NO_AUTOMATIC_COMBAT_ANCHOR = 0x00000020,
    ONLY_TARGETABLE_BY_CREATOR = 0x00000040,
    TREAT_AS_PLAYER_FOR_ISPLAYERCONTROLLED = 0x00000080,
    GENERATE_NO_THREAT_OR_DAMAGE = 0x00000100,
    INTERACT_ONLY_ON_QUEST = 0x00000200,
    DISABLE_KILL_CREDIT_FOR_OFFLINE_PLAYERS = 0x00000400,
    AI_ADDITIONAL_PATHING = 0x00080000
}