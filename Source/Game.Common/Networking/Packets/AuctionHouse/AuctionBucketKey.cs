// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.AuctionHouse;

public class AuctionBucketKey
{
	public uint ItemID;
	public ushort ItemLevel;
	public ushort? BattlePetSpeciesID;
	public ushort? SuffixItemNameDescriptionID;

	public AuctionBucketKey() { }

	public AuctionBucketKey(AuctionsBucketKey key)
	{
		ItemID = key.ItemId;
		ItemLevel = key.ItemLevel;

		if (key.BattlePetSpeciesId != 0)
			BattlePetSpeciesID = key.BattlePetSpeciesId;

		if (key.SuffixItemNameDescriptionId != 0)
			SuffixItemNameDescriptionID = key.SuffixItemNameDescriptionId;
	}

	public AuctionBucketKey(WorldPacket data)
	{
		data.ResetBitPos();
		ItemID = data.ReadBits<uint>(20);
		var hasBattlePetSpeciesId = data.HasBit();
		ItemLevel = data.ReadBits<ushort>(11);
		var hasSuffixItemNameDescriptionId = data.HasBit();

		if (hasBattlePetSpeciesId)
			BattlePetSpeciesID = data.ReadUInt16();

		if (hasSuffixItemNameDescriptionId)
			SuffixItemNameDescriptionID = data.ReadUInt16();
	}

	public void Write(WorldPacket data)
	{
		data.WriteBits(ItemID, 20);
		data.WriteBit(BattlePetSpeciesID.HasValue);
		data.WriteBits(ItemLevel, 11);
		data.WriteBit(SuffixItemNameDescriptionID.HasValue);
		data.FlushBits();

		if (BattlePetSpeciesID.HasValue)
			data.WriteUInt16(BattlePetSpeciesID.Value);

		if (SuffixItemNameDescriptionID.HasValue)
			data.WriteUInt16(SuffixItemNameDescriptionID.Value);
	}
}
