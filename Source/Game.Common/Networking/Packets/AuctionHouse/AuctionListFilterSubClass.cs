// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.AuctionHouse;

public struct AuctionListFilterSubClass
{
	public int ItemSubclass;
	public ulong InvTypeMask;

	public AuctionListFilterSubClass(WorldPacket data)
	{
		InvTypeMask = data.ReadUInt64();
		ItemSubclass = data.ReadInt32();
	}
}
