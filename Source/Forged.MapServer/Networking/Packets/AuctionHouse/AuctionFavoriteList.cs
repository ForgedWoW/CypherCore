// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

class AuctionFavoriteList : ServerPacket
{
	public uint DesiredDelay;
	public List<AuctionFavoriteInfo> Items = new();

	public AuctionFavoriteList() : base(ServerOpcodes.AuctionFavoriteList) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(DesiredDelay);
		_worldPacket.WriteBits(Items.Count, 7);
		_worldPacket.FlushBits();

		foreach (var favoriteInfo in Items)
			favoriteInfo.Write(_worldPacket);
	}
}