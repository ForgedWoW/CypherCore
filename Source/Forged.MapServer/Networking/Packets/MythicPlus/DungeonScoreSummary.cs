// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.MythicPlus;

public class DungeonScoreSummary
{
    public float OverallScoreCurrentSeason;
    public float LadderScoreCurrentSeason;
    public List<DungeonScoreMapSummary> Runs = new();

    public void Write(WorldPacket data)
    {
        data.WriteFloat(OverallScoreCurrentSeason);
        data.WriteFloat(LadderScoreCurrentSeason);
        data.WriteInt32(Runs.Count);

        foreach (var dungeonScoreMapSummary in Runs)
            dungeonScoreMapSummary.Write(data);
    }
}