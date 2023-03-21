// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public struct LfgPlayerQuestRewardCurrency
{
	public LfgPlayerQuestRewardCurrency(uint currencyId, uint quantity)
	{
		CurrencyID = currencyId;
		Quantity = quantity;
	}

	public uint CurrencyID;
	public uint Quantity;
}