// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal struct GarrisonMissionReward
{
	public void Write(WorldPacket data)
	{
		data.WriteInt32(ItemID);
		data.WriteUInt32(ItemQuantity);
		data.WriteInt32(CurrencyID);
		data.WriteUInt32(CurrencyQuantity);
		data.WriteUInt32(FollowerXP);
		data.WriteUInt32(GarrMssnBonusAbilityID);
		data.WriteInt32(ItemFileDataID);
		data.WriteBit(ItemInstance != null);
		data.FlushBits();

		if (ItemInstance != null)
			ItemInstance.Write(data);
	}

	public int ItemID;
	public uint ItemQuantity;
	public int CurrencyID;
	public uint CurrencyQuantity;
	public uint FollowerXP;
	public uint GarrMssnBonusAbilityID;
	public int ItemFileDataID;
	public ItemInstance ItemInstance;
}