// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
using Game.Common.Networking.Packets.Item;

namespace Game.Common.Networking.Packets.BlackMarket;

public class BlackMarketOutbid : ServerPacket
{
	public uint MarketID;
	public ItemInstance Item;
	public uint RandomPropertiesID;
	public BlackMarketOutbid() : base(ServerOpcodes.BlackMarketOutbid) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(MarketID);
		_worldPacket.WriteUInt32(RandomPropertiesID);
		Item.Write(_worldPacket);
	}
}
