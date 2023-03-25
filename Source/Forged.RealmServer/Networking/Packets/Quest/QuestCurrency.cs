// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public struct QuestCurrency
{
	public QuestCurrency(uint currencyID = 0, int amount = 0)
	{
		CurrencyID = currencyID;
		Amount = amount;
	}

	public uint CurrencyID;
	public int Amount;
}