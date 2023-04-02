// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Quest;

internal struct WorldQuestUpdateInfo
{
    public long LastUpdate;

    public uint QuestID;

    public uint Timer;

    public int Value;

    // WorldState
    public int VariableID;

    public WorldQuestUpdateInfo(long lastUpdate, uint questID, uint timer, int variableID, int value)
    {
        LastUpdate = lastUpdate;
        QuestID = questID;
        Timer = timer;
        VariableID = variableID;
        Value = value;
    }
}