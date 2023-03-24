// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Common.Networking.Packets.BlackMarket;

public class BlackMarketRequestItemsResult : ServerPacket
{
	public long LastUpdateID;
	public List<BlackMarketItem> Items = new();
	public BlackMarketRequestItemsResult() : base(ServerOpcodes.BlackMarketRequestItemsResult) { }

	public override void Write()
	{
		_worldPacket.WriteInt64(LastUpdateID);
		_worldPacket.WriteInt32(Items.Count);

		foreach (var item in Items)
			item.Write(_worldPacket);
	}
}
