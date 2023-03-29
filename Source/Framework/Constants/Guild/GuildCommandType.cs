// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum GuildCommandType
{
    CreateGuild = 0,
    InvitePlayer = 1,
    LeaveGuild = 3,
    GetRoster = 5,
    PromotePlayer = 6,
    DemotePlayer = 7,
    RemovePlayer = 8,
    ChangeLeader = 10,
    EditMOTD = 11,
    GuildChat = 13,
    Founder = 14,
    ChangeRank = 16,
    EditPublicNote = 19,
    ViewTab = 21,
    MoveItem = 22,
    Repair = 25
}