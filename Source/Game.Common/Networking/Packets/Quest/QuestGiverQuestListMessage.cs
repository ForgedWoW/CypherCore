// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Entities.Objects;
using Game.Common.Networking.Packets.NPC;

namespace Game.Common.Networking.Packets.Quest;

public class QuestGiverQuestListMessage : ServerPacket
{
	public ObjectGuid QuestGiverGUID;
	public uint GreetEmoteDelay;
	public uint GreetEmoteType;
	public List<ClientGossipText> QuestDataText = new();
	public string Greeting = "";
	public QuestGiverQuestListMessage() : base(ServerOpcodes.QuestGiverQuestListMessage) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(QuestGiverGUID);
		_worldPacket.WriteUInt32(GreetEmoteDelay);
		_worldPacket.WriteUInt32(GreetEmoteType);
		_worldPacket.WriteInt32(QuestDataText.Count);
		_worldPacket.WriteBits(Greeting.GetByteCount(), 11);
		_worldPacket.FlushBits();

		foreach (var gossip in QuestDataText)
			gossip.Write(_worldPacket);

		_worldPacket.WriteString(Greeting);
	}
}
