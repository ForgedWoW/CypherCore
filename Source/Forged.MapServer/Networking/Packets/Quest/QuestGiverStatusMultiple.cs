// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

public class QuestGiverStatusMultiple : ServerPacket
{
	public List<QuestGiverInfo> QuestGiver = new();
	public QuestGiverStatusMultiple() : base(ServerOpcodes.QuestGiverStatusMultiple, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(QuestGiver.Count);

		foreach (var questGiver in QuestGiver)
		{
			_worldPacket.WritePackedGuid(questGiver.Guid);
			_worldPacket.WriteUInt32((uint)questGiver.Status);
		}
	}
}