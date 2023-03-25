// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

class BlackMarketBidOnItemResult : ServerPacket
{
	public uint MarketID;
	public ItemInstance Item;
	public BlackMarketError Result;
	public BlackMarketBidOnItemResult() : base(ServerOpcodes.BlackMarketBidOnItemResult) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(MarketID);
		_worldPacket.WriteUInt32((uint)Result);
		Item.Write(_worldPacket);
	}
}