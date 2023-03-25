// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Loot;

class MasterLootItem : ClientPacket
{
	public Array<LootRequest> Loot = new(1000);
	public ObjectGuid Target;
	public MasterLootItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var Count = _worldPacket.ReadUInt32();
		Target = _worldPacket.ReadPackedGuid();

		for (var i = 0; i < Count; ++i)
		{
			LootRequest lootRequest = new()
			{
				Object = _worldPacket.ReadPackedGuid(),
				LootListID = _worldPacket.ReadUInt8()
			};

			Loot[i] = lootRequest;
		}
	}
}