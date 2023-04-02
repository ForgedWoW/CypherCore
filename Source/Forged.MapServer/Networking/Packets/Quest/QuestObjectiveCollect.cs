// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Quest;

public struct QuestObjectiveCollect
{
    public int Amount;

    public uint Flags;

    public uint ObjectID;

    public QuestObjectiveCollect(uint objectID = 0, int amount = 0, uint flags = 0)
    {
        ObjectID = objectID;
        Amount = amount;
        Flags = flags;
    }
}