// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public struct LFGPlayerRewards
{
	public LFGPlayerRewards(uint id, uint quantity, int bonusQuantity, bool isCurrency)
	{
		Quantity = quantity;
		BonusQuantity = bonusQuantity;
		RewardItem = null;
		RewardCurrency = null;

		if (!isCurrency)
		{
			RewardItem = new ItemInstance();
			RewardItem.ItemID = id;
		}
		else
		{
			RewardCurrency = id;
		}
	}

	public void Write(WorldPacket data)
	{
		data.WriteBit(RewardItem != null);
		data.WriteBit(RewardCurrency.HasValue);

		if (RewardItem != null)
			RewardItem.Write(data);

		data.WriteUInt32(Quantity);
		data.WriteInt32(BonusQuantity);

		if (RewardCurrency.HasValue)
			data.WriteUInt32(RewardCurrency.Value);
	}

	public ItemInstance RewardItem;
	public uint? RewardCurrency;
	public uint Quantity;
	public int BonusQuantity;
}