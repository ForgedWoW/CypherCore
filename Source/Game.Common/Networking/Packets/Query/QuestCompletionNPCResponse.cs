// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

public class QuestCompletionNPCResponse : ServerPacket
{
	public List<QuestCompletionNPC> QuestCompletionNPCs = new();
	public QuestCompletionNPCResponse() : base(ServerOpcodes.QuestCompletionNpcResponse, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(QuestCompletionNPCs.Count);

		foreach (var quest in QuestCompletionNPCs)
		{
			_worldPacket.WriteUInt32(quest.QuestID);

			_worldPacket.WriteInt32(quest.NPCs.Count);

			foreach (var npc in quest.NPCs)
				_worldPacket.WriteUInt32(npc);
		}
	}
}