// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.MythicPlus;

public struct DungeonScoreMapSummary
{
    public int BestRunDurationMS;
    public int BestRunLevel;
    public int ChallengeModeID;
    public bool FinishedSuccess;
    public float MapScore;
    public void Write(WorldPacket data)
    {
        data.WriteInt32(ChallengeModeID);
        data.WriteFloat(MapScore);
        data.WriteInt32(BestRunLevel);
        data.WriteInt32(BestRunDurationMS);
        data.WriteBit(FinishedSuccess);
        data.FlushBits();
    }
}