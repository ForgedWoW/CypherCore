// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum LfgUpdateType
{
    Default = 0,    // Internal Use
    LeaderUnk1 = 1, // Fixme: At Group Leave
    RolecheckAborted = 4,
    JoinQueue = 6,
    RolecheckFailed = 7,
    RemovedFromQueue = 8,
    ProposalFailed = 9,
    ProposalDeclined = 10,
    GroupFound = 11,
    AddedToQueue = 13,
    SuspendedQueue = 14,
    ProposalBegin = 15,
    UpdateStatus = 16,
    GroupMemberOffline = 17,
    GroupDisbandUnk16 = 18, // FIXME: Sometimes at group disband
    JoinQueueInitial = 25,
    DungeonFinished = 26,
    PartyRoleNotAvailable = 46,
    JoinLfgObjectFailed = 48,
    RemovedLevelup = 49,
    RemovedXpToggle = 50,
    RemovedFactionChange = 51
}