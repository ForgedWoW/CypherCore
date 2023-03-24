// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.Item;

namespace Game.Common.Networking.Packets.BlackMarket;

public class BlackMarketWon : ServerPacket
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
