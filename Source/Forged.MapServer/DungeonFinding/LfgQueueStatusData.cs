// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DungeonFinding;

public class LfgQueueStatusData
{
    public byte Dps { get; set; }
    public uint DungeonId { get; set; }
    public byte Healers { get; set; }
    public uint QueuedTime { get; set; }
    public byte QueueId { get; set; }
    public byte Tanks { get; set; }
    public int WaitTime { get; set; }
    public int WaitTimeAvg { get; set; }
    public int WaitTimeDps { get; set; }
    public int WaitTimeHealer { get; set; }
    public int WaitTimeTank { get; set; }

    public LfgQueueStatusData(byte queueId = 0, uint dungeonId = 0, int waitTime = -1, int waitTimeAvg = -1, int waitTimeTank = -1, int waitTimeHealer = -1,
                              int waitTimeDps = -1, uint queuedTime = 0, byte tanks = 0, byte healers = 0, byte dps = 0)
    {
        QueueId = queueId;
        DungeonId = dungeonId;
        WaitTime = waitTime;
        WaitTimeAvg = waitTimeAvg;
        WaitTimeTank = waitTimeTank;
        WaitTimeHealer = waitTimeHealer;
        WaitTimeDps = waitTimeDps;
        QueuedTime = queuedTime;
        Tanks = tanks;
        Healers = healers;
        Dps = dps;
    }
}