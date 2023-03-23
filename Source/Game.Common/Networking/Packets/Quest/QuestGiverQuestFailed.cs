﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Quest;

public class QuestGiverQuestFailed : ServerPacket
{
	public uint QuestID;
	public InventoryResult Reason;
	public QuestGiverQuestFailed() : base(ServerOpcodes.QuestGiverQuestFailed) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(QuestID);
		_worldPacket.WriteUInt32((uint)Reason);
	}
}
