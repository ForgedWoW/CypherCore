// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

internal class QuestGiverQuestFailed : ServerPacket
{
    public uint QuestID;
    public InventoryResult Reason;
    public QuestGiverQuestFailed() : base(ServerOpcodes.QuestGiverQuestFailed) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(QuestID);
        WorldPacket.WriteUInt32((uint)Reason);
    }
}