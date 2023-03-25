﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

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