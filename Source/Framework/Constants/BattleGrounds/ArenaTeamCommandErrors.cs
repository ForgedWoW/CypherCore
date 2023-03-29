// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ArenaTeamCommandErrors
{
    ArenaTeamCreated = 0x00,
    ArenaTeamInternal = 0x01,
    AlreadyInArenaTeam = 0x02,
    AlreadyInArenaTeamS = 0x03,
    InvitedToArenaTeam = 0x04,
    AlreadyInvitedToArenaTeamS = 0x05,
    ArenaTeamNameInvalid = 0x06,
    ArenaTeamNameExistsS = 0x07,
    ArenaTeamLeaderLeaveS = 0x08,
    ArenaTeamPermissions = 0x08,
    ArenaTeamPlayerNotInTeam = 0x09,
    ArenaTeamPlayerNotInTeamSs = 0x0a,
    ArenaTeamPlayerNotFoundS = 0x0b,
    ArenaTeamNotAllied = 0x0c,
    ArenaTeamIgnoringYouS = 0x13,
    ArenaTeamTargetTooLowS = 0x15,
    ArenaTeamTargetTooHighS = 0x16,
    ArenaTeamTooManyMembersS = 0x17,
    ArenaTeamNotFound = 0x1b,
    ArenaTeamsLocked = 0x1e,
    ArenaTeamTooManyCreate = 0x21,
}