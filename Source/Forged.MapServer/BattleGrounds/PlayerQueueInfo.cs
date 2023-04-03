// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.BattleGrounds;

/// <summary>
///     stores information for players in queue
/// </summary>
public class PlayerQueueInfo
{
    public GroupQueueInfo GroupInfo;

    public uint LastOnlineTime; // for tracking and removing offline players from queue after 5 minutes
    // pointer to the associated groupqueueinfo
}