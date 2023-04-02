// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Quest;

public struct QuestCompleteDisplaySpell
{
    public uint PlayerConditionID;
    public uint SpellID;
    public QuestCompleteDisplaySpell(uint spellID, uint playerConditionID)
    {
        SpellID = spellID;
        PlayerConditionID = playerConditionID;
    }

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(SpellID);
        data.WriteUInt32(PlayerConditionID);
    }
}