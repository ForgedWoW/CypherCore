// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Quest;

public class QuestGiverCompleteQuest : ClientPacket
{
    public bool FromScript;
    public ObjectGuid QuestGiverGUID; // NPC / GameObject guid for normal quest completion. Player guid for self-completed quests
    public uint QuestID;
     // 0 - standart complete quest mode with npc, 1 - auto-complete mode
    public QuestGiverCompleteQuest(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        QuestGiverGUID = WorldPacket.ReadPackedGuid();
        QuestID = WorldPacket.ReadUInt32();
        FromScript = WorldPacket.HasBit();
    }
}