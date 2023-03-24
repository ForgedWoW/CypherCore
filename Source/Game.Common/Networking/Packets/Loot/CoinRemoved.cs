// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Loot;

public class CoinRemoved : ServerPacket
{
	public ObjectGuid LootObj;
	public CoinRemoved() : base(ServerOpcodes.CoinRemoved) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
	}
}
