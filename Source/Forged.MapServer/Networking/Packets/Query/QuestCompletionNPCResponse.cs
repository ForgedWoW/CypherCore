// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Query;

internal class QuestCompletionNPCResponse : ServerPacket
{
    public List<QuestCompletionNPC> QuestCompletionNPCs = new();
    public QuestCompletionNPCResponse() : base(ServerOpcodes.QuestCompletionNpcResponse, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(QuestCompletionNPCs.Count);

        foreach (var quest in QuestCompletionNPCs)
        {
            WorldPacket.WriteUInt32(quest.QuestID);

            WorldPacket.WriteInt32(quest.NPCs.Count);

            foreach (var npc in quest.NPCs)
                WorldPacket.WriteUInt32(npc);
        }
    }
}