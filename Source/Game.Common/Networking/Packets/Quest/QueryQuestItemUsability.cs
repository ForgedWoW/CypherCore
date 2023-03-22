﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Networking.Packets.Quest;

public class QueryQuestItemUsability : ClientPacket
{
	public ObjectGuid CreatureGUID;
	public List<ObjectGuid> ItemGUIDs = new();

	public QueryQuestItemUsability(WorldPacket worldPacket) : base(worldPacket) { }

	public override void Read()
	{
		CreatureGUID = _worldPacket.ReadPackedGuid();
		var itemGuidCount = _worldPacket.ReadUInt32();

		for (var i = 0; i < itemGuidCount; ++i)
			ItemGUIDs.Add(_worldPacket.ReadPackedGuid());
	}
}