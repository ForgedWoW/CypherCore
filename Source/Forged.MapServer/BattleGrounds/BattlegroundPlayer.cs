// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.BattleGrounds;

public class BattlegroundPlayer
{
    public int ActiveSpec;

    // Player's active spec
    public bool Mercenary;

    public long OfflineRemoveTime; // for tracking and removing offline players from queue after 5 Time.Minutes
    public TeamFaction Team;       // Player's team
}