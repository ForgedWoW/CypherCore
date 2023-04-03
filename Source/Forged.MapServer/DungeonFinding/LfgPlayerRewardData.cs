// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DungeonFinding;

public class LfgPlayerRewardData
{
    public bool Done;
    public Quest.Quest Quest;
    public uint RdungeonEntry;
    public uint SdungeonEntry;

    public LfgPlayerRewardData(uint random, uint current, bool done, Quest.Quest quest)
    {
        RdungeonEntry = random;
        SdungeonEntry = current;
        Done = done;
        Quest = quest;
    }
}