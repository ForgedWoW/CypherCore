// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.Q;

public class QuestPOIBlobEntry
{
    public int ID;
    public short MapID;
    public int UiMapID;
    public byte NumPoints;
    public uint QuestID;
    public int ObjectiveIndex;
    public int ObjectiveID;
    public uint PlayerConditionID;
    public uint UNK_9_0_1;
}