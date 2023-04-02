// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.NPC;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

public class QuestGiverQuestListMessage : ServerPacket
{
    public uint GreetEmoteDelay;
    public uint GreetEmoteType;
    public string Greeting = "";
    public List<ClientGossipText> QuestDataText = new();
    public ObjectGuid QuestGiverGUID;
    public QuestGiverQuestListMessage() : base(ServerOpcodes.QuestGiverQuestListMessage) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(QuestGiverGUID);
        WorldPacket.WriteUInt32(GreetEmoteDelay);
        WorldPacket.WriteUInt32(GreetEmoteType);
        WorldPacket.WriteInt32(QuestDataText.Count);
        WorldPacket.WriteBits(Greeting.GetByteCount(), 11);
        WorldPacket.FlushBits();

        foreach (var gossip in QuestDataText)
            gossip.Write(WorldPacket);

        WorldPacket.WriteString(Greeting);
    }
}