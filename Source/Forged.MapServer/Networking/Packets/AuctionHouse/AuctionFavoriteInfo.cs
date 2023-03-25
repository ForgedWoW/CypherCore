// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

public struct AuctionFavoriteInfo
{
	public uint Order;
	public uint ItemID;
	public uint ItemLevel;
	public uint BattlePetSpeciesID;
	public uint SuffixItemNameDescriptionID;

	public AuctionFavoriteInfo(WorldPacket data)
	{
		Order = data.ReadUInt32();
		ItemID = data.ReadUInt32();
		ItemLevel = data.ReadUInt32();
		BattlePetSpeciesID = data.ReadUInt32();
		SuffixItemNameDescriptionID = data.ReadUInt32();
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(Order);
		data.WriteUInt32(ItemID);
		data.WriteUInt32(ItemLevel);
		data.WriteUInt32(BattlePetSpeciesID);
		data.WriteUInt32(SuffixItemNameDescriptionID);
	}
}