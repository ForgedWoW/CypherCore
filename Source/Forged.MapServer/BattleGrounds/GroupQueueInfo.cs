// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.BattleGrounds;

/// <summary>
///     stores information about the group in queue (also used when joined as solo!)
/// </summary>
public class GroupQueueInfo
{
    public uint ArenaMatchmakerRating;
    public uint ArenaTeamId;
    public uint ArenaTeamRating;

    public uint IsInvitedToBGInstanceGUID;

    // team id if rated match
    public uint JoinTime;

    public uint OpponentsMatchmakerRating;

    // was invited to certain BG
    // if rated match, inited to the rating of the team
    // if rated match, inited to the rating of the team
    public uint OpponentsTeamRating;

    public Dictionary<ObjectGuid, PlayerQueueInfo> Players = new(); // player queue info map

    // time when group was added
    public uint RemoveInviteTime;

    public TeamFaction Team; // Player team (ALLIANCE/HORDE)
    // time when we will remove invite for players in group
    // for rated arena matches
    // for rated arena matches
}