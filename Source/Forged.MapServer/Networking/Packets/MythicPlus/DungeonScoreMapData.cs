// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.MythicPlus;

public class DungeonScoreMapData
{
    public int MapChallengeModeID;
    public List<DungeonScoreBestRunForAffix> BestRuns = new();
    public float OverAllScore;

    public void Write(WorldPacket data)
    {
        data.WriteInt32(MapChallengeModeID);
        data.WriteInt32(BestRuns.Count);
        data.WriteFloat(OverAllScore);

        foreach (var bestRun in BestRuns)
            bestRun.Write(data);
    }
}