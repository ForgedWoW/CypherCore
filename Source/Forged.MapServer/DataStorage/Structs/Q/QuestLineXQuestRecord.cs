// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.Q;

public sealed class QuestLineXQuestRecord
{
    public uint Id;
    public uint QuestLineID;
    public uint QuestID;
    public uint OrderIndex;
    public int Flags;
}