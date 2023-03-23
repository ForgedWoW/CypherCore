// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Loot;

public struct LootCurrency
{
	public uint CurrencyID;
	public uint Quantity;
	public byte LootListID;
	public byte UIType;
}
