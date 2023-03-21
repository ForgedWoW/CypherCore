// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class TransmogrifyItems : ClientPacket
{
	public ObjectGuid Npc;
	public Array<TransmogrifyItem> Items = new(13);
	public bool CurrentSpecOnly;
	public TransmogrifyItems(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var itemsCount = _worldPacket.ReadUInt32();
		Npc = _worldPacket.ReadPackedGuid();

		for (var i = 0; i < itemsCount; ++i)
		{
			TransmogrifyItem item = new();
			item.Read(_worldPacket);
			Items[i] = item;
		}

		CurrentSpecOnly = _worldPacket.HasBit();
	}
}