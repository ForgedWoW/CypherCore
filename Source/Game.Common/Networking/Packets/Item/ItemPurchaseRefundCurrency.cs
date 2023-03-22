﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

struct ItemPurchaseRefundCurrency
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt32(CurrencyID);
		data.WriteUInt32(CurrencyCount);
	}

	public uint CurrencyID;
	public uint CurrencyCount;
}