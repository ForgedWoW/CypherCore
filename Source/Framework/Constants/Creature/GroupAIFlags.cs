// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum GroupAIFlags
{
    None = 0,                                                          // No creature group behavior
    MembersAssistLeader = 0x01,                                        // The member aggroes if the leader aggroes
    LeaderAssistsMember = 0x02,                                        // The leader aggroes if the member aggroes
    MembersAssistMember = (MembersAssistLeader | LeaderAssistsMember), // every member will assist if any member is attacked
    IdleInFormation = 0x200,                                           // The member will follow the leader when pathing idly
}