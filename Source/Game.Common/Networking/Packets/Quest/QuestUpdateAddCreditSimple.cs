﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Quest;

public class QuestUpdateAddCreditSimple : ServerPacket
{
	public uint QuestID;
	public int ObjectID;
	public QuestObjectiveType ObjectiveType;
	public QuestUpdateAddCreditSimple() : base(ServerOpcodes.QuestUpdateAddCreditSimple, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(QuestID);
		_worldPacket.WriteInt32(ObjectID);
		_worldPacket.WriteUInt8((byte)ObjectiveType);
	}
}
