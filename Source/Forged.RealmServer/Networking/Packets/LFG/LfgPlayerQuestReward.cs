// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.RealmServer.Networking.Packets;

public class LfgPlayerQuestReward
{
	public uint Mask;
	public uint RewardMoney;
	public uint RewardXP;
	public List<LfgPlayerQuestRewardItem> Item = new();
	public List<LfgPlayerQuestRewardCurrency> Currency = new();
	public List<LfgPlayerQuestRewardCurrency> BonusCurrency = new();
	public int? RewardSpellID; // Only used by SMSG_LFG_PLAYER_INFO
	public int? Unused1;
	public ulong? Unused2;
	public int? Honor; // Only used by SMSG_REQUEST_PVP_REWARDS_RESPONSE

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(Mask);
		data.WriteUInt32(RewardMoney);
		data.WriteUInt32(RewardXP);
		data.WriteInt32(Item.Count);
		data.WriteInt32(Currency.Count);
		data.WriteInt32(BonusCurrency.Count);

		// Item
		foreach (var item in Item)
		{
			data.WriteUInt32(item.ItemID);
			data.WriteUInt32(item.Quantity);
		}

		// Currency
		foreach (var currency in Currency)
		{
			data.WriteUInt32(currency.CurrencyID);
			data.WriteUInt32(currency.Quantity);
		}

		// BonusCurrency
		foreach (var bonusCurrency in BonusCurrency)
		{
			data.WriteUInt32(bonusCurrency.CurrencyID);
			data.WriteUInt32(bonusCurrency.Quantity);
		}

		data.WriteBit(RewardSpellID.HasValue);
		data.WriteBit(Unused1.HasValue);
		data.WriteBit(Unused2.HasValue);
		data.WriteBit(Honor.HasValue);
		data.FlushBits();

		if (RewardSpellID.HasValue)
			data.WriteInt32(RewardSpellID.Value);

		if (Unused1.HasValue)
			data.WriteInt32(Unused1.Value);

		if (Unused2.HasValue)
			data.WriteUInt64(Unused2.Value);

		if (Honor.HasValue)
			data.WriteInt32(Honor.Value);
	}
}