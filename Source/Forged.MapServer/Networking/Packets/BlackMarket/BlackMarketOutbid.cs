// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BlackMarket;

class BlackMarketOutbid : ServerPacket
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