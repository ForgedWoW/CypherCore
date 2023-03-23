// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Loot;

namespace Game.Common.Networking.Packets.Loot;

public class LootAllPassed : ServerPacket
{
	public ObjectGuid LootObj;
	public LootItemData Item = new();
	public LootAllPassed() : base(ServerOpcodes.LootAllPassed) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
		Item.Write(_worldPacket);
	}
}
