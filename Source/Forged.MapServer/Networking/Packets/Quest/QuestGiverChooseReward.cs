// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Quest;

public class QuestGiverChooseReward : ClientPacket
{
    public QuestChoiceItem Choice;
    public ObjectGuid QuestGiverGUID;
    public uint QuestID;
    public QuestGiverChooseReward(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        QuestGiverGUID = WorldPacket.ReadPackedGuid();
        QuestID = WorldPacket.ReadUInt32();
        Choice.Read(WorldPacket);
    }
}