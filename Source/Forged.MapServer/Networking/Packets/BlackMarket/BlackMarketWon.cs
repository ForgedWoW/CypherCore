// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BlackMarket;

class BlackMarketWon : ServerPacket
{
	public uint MarketID;
	public ItemInstance Item;
	public int RandomPropertiesID;
	public BlackMarketWon() : base(ServerOpcodes.BlackMarketWon) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(MarketID);
		_worldPacket.WriteInt32(RandomPropertiesID);
		Item.Write(_worldPacket);
	}
}