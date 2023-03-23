﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Quest;

public class QuestPushResult : ClientPacket
{
	public ObjectGuid SenderGUID;
	public uint QuestID;
	public QuestPushReason Result;
	public QuestPushResult(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SenderGUID = _worldPacket.ReadPackedGuid();
		QuestID = _worldPacket.ReadUInt32();
		Result = (QuestPushReason)_worldPacket.ReadUInt8();
	}
}
